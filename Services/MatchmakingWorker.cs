using MatchmakingEngine.Domain;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using MatchmakingEngine.Data;
using Microsoft.AspNetCore.SignalR;
using MatchmakingEngine.Hubs;

namespace MatchmakingEngine.Services;

public class MatchmakingWorker : BackgroundService
{
    private readonly IMatchmakingQueue _queue;
    private readonly ILogger<MatchmakingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MatchmakingHub> _hubContext;

    private readonly List<MatchmakingTicket> _waitingRoom = new();

    private const double MaxMmrDifference = 100.0;
    private const double MaxTrustDifference = 0.3;
    public MatchmakingWorker(
        IMatchmakingQueue queue,
        ILogger<MatchmakingWorker> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<MatchmakingHub> hubContext
        )
    {
        _queue = queue;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Engine] async Matchmaking worker initialized.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var newTicket = await _queue.DequeueAsync(stoppingToken);

                if (newTicket != null)
                {
                    _logger.LogInformation("[Queue] Ticket dequeued for player {Username} (MMR: {Mmr}, Region: {Region}). Ready for match evaluation!",
                        newTicket.Username, newTicket.Mmr, newTicket.Region);

                    await TryMatchPlayerAsync(newTicket);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[Engine] Matchmaking worker is shutting down gracefully.");
        }

    }

    private async Task TryMatchPlayerAsync(MatchmakingTicket newTicket)
    {
        var opponent = _waitingRoom.FirstOrDefault(waitingTicket =>
        waitingTicket.Region == newTicket.Region &&
        Math.Abs(waitingTicket.Mmr - newTicket.Mmr) <= MaxMmrDifference &&
        Math.Abs(waitingTicket.TrustFactor - newTicket.TrustFactor) <= MaxTrustDifference);

        if (opponent != null)
        {
            _waitingRoom.Remove(opponent);
            _queue.RemovePlayer(newTicket.PlayerId);
            _queue.RemovePlayer(opponent.PlayerId);

            var lobbyId = Guid.NewGuid();
            var match = new Match(
                Id: lobbyId,
                Player1Id: newTicket.PlayerId,
                Player2Id: opponent.PlayerId,
                AverageMmr: (newTicket.Mmr + opponent.Mmr) / 2.0,
                CreatedAt: DateTimeOffset.UtcNow
                );

            // Dbcontext - scoper, worker singletone => scopefactory for temporary scope
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();

                dbContext.Matches.Add(match);
                await dbContext.SaveChangesAsync();
            }

            var matchPayload = new
            {
                LobbyId = lobbyId,
                AverageMmr = match.AverageMmr,
                Message = "Match found! Please accept the match."
            };

            await _hubContext.Clients.Group(newTicket.PlayerId.ToString())
                .SendAsync("MatchFound", matchPayload);

            await _hubContext.Clients.Group(opponent.PlayerId.ToString())
                .SendAsync("MatchFound", matchPayload);

            _logger.LogWarning("[MATCH FOUND] Lobby {LobbyId} created! Player [{P1}] (MMR: {M1}) vs Player [{P2}] (MMR: {M2}) on Region {Region}!",
                lobbyId, newTicket.Username, newTicket.Mmr, opponent.Username, opponent.Mmr, newTicket.Region);
        }
        else
        {
            _waitingRoom.Add(newTicket);
            _logger.LogInformation("[Waiting Room] No match found for {Username}. Added to waiting room. (Total waiting: {Count}",
                newTicket.Username, _waitingRoom.Count);
        }
    }
}
