using MatchmakingEngine.Application.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

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

                var timedOutMatches = await matchRepo.GetMatchesInVetoTimeoutAsync(DateTimeOffset.UtcNow, cancellationToken);

                foreach (var match in timedOutMatches)
                {
                    if (match.AvailableMaps.Any() && match.CurrentVetoTurnPlayerId.HasValue)
                    {
                        var randomMap = match.AvailableMaps[new Random().Next(match.AvailableMaps.Count)];

                        _logger.LogWarning("[VetoWorker] Player {PlayerId} AFK! Auto-banning map {MapName} for Match {MatchId}",
                            match.CurrentVetoTurnPlayerId, randomMap, match.Id);

                        await meditor.Send(new BanMapCommand(match.Id, match.CurrentVetoTurnPlayerId.Value, randomMap), cancellationToken);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        await Task.Delay(5000, cancellationToken);
    }
}
