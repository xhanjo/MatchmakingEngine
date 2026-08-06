using MatchmakingEngine.Application.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace MatchmakingEngine.HostedServices;

public class MapVetoWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MapVetoWorker> _logger;

    public MapVetoWorker(IServiceProvider serviceProvider, ILogger<MapVetoWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[VetoWorker] Started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var matchRepo = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
                var meditor = scope.ServiceProvider.GetRequiredService<IMediator>();

                var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                var db = redis.GetDatabase();

                var lockKey = "lock:map_veto_worker";
                var lockToken = Guid.NewGuid().ToString();

                if (await db.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(4)))
                {
                    try
                    {
                        var timedOutMatches = await matchRepo.GetMatchesInVetoTimeoutAsync(DateTimeOffset.UtcNow, cancellationToken);

                        foreach (var match in timedOutMatches)
                        {
                            if (match.AvailableMaps.Any() && match.CurrentVetoTurnPlayerId.HasValue)
                            {
                                var randomMap = match.AvailableMaps[new Random().Next(match.AvailableMaps.Count)];

                                _logger.LogWarning("[VetoWorker] Player {PlayerId} AFK! Auto-banning map {MapName} for Match {MatchId}",
                                    match.CurrentVetoTurnPlayerId, randomMap, match.Id);

                                var result = await meditor.Send(new BanMapCommand(match.Id, match.CurrentVetoTurnPlayerId.Value, randomMap), cancellationToken);

                                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<MatchmakingHub>>();
                                foreach (var pid in result.PlayerIds)
                                {
                                    await hubContext.Clients.Group(pid.ToString()).SendAsync("MapVetoUpdated", result.VetoState);
                                }
                            }
                        }
                    } 
                    finally
                    {
                        await db.LockReleaseAsync(lockKey, lockToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VetoWorker");
            }
            await Task.Delay(5000, cancellationToken);
        }
    }
}
