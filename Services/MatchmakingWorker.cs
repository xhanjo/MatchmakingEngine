namespace MatchmakingEngine.Services;

public class MatchmakingWorker : BackgroundService
{
    private readonly IMatchmakingQueue _queue;
    private readonly ILogger<MatchmakingWorker> _logger;
    public MatchmakingWorker(IMatchmakingQueue queue, ILogger<MatchmakingWorker> logger)
    {
        _queue = queue;
        _logger = logger;

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Engine] async Matchmaking worker initialized.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var ticket = await _queue.DequeueAsync(stoppingToken);

            if (ticket != null)
            {
                _logger.LogInformation("[Queue] Ticket dequeued for player {Username} (MMR: {Mmr}, Region: {Region}). Ready for match evaluation!",
                    ticket.Username, ticket.Mmr, ticket.Region);
            }
        }
    }
}
