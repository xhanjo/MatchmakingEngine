using MatchmakingEngine.Domain;
using System.Transactions;

namespace MatchmakingEngine.Services;

public class MatchmakingWorker : BackgroundService
{
    private readonly IMatchmakingQueue _queue;
    private readonly ILogger<MatchmakingWorker> _logger;

    private readonly List<MatchmakingTicket> _waitingRoom = new();

    private const double MaxMmrDifference = 100.0;
    private const double MaxTrustDifference = 0.3;
    public MatchmakingWorker(IMatchmakingQueue queue, ILogger<MatchmakingWorker> logger)
    {
        _queue = queue;
        _logger = logger;

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

                    TryMatchPlayer(newTicket);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[Engine] Matchmaking worker is shutting down gracefully.");
        }

    }

    private void TryMatchPlayer(MatchmakingTicket newTicket)
    {
        var opponent = _waitingRoom.FirstOrDefault(waitingTicket =>
        waitingTicket.Region == newTicket.Region &&
        Math.Abs(waitingTicket.Mmr - newTicket.Mmr) <= MaxMmrDifference &&
        Math.Abs(waitingTicket.TrustFactor - newTicket.TrustFactor) <= MaxTrustDifference);

        if (opponent != null)
        {
            _waitingRoom.Remove(opponent);

            var lobbyId = Guid.NewGuid();

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
