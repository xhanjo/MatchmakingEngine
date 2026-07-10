using MatchmakingEngine.Domain;
using MatchmakingEngine.Data;
using Microsoft.AspNetCore.SignalR;
using MatchmakingEngine.Hubs;
using Microsoft.AspNetCore.Components.Forms;

namespace MatchmakingEngine.Services;

public class MatchmakingWorker : BackgroundService
{
    private readonly IMatchmakingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchmakingWorker> _logger;
    private readonly IHubContext<MatchmakingHub> _hubContext;

    private const double MaxMmrDifference = 100.0;
    private const double MaxTrustDifference = 0.3;
    public MatchmakingWorker(
        IMatchmakingQueue queue,
        ILogger<MatchmakingWorker> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<MatchmakingHub> hubContext)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Engine] Smart Matchmaking worker with Redis ZSET initialized.");


        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var anchorId = await _queue.DequeueEvaluationIdAsync(stoppingToken);

                if (anchorId.HasValue)
                    await ProcessEvaluationAsync(anchorId.Value, stoppingToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured during matchmaking cycle.");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }


    private async Task ProcessEvaluationAsync(Guid anchorId, CancellationToken cancellationToken)
    {
        var anchor = await _queue.GetTicketAsync(anchorId);
        if (anchor == null) return;

        var waitTimeSeconds = DateTimeOffset.UtcNow - anchor.EnqueuedAt;

        var currentDelta = Math.Min(300, 50 + waitTimeSeconds.TotalSeconds * 5);

        var minMmr = anchor.Mmr - currentDelta;
        var maxMmr = anchor.Mmr + currentDelta;

        var candidateIds = await _queue.GetCandidatesByMmrRangeAsync(anchor.Region, minMmr, maxMmr);

        MatchmakingTicket? opponent = null;

        foreach (var candidate in candidateIds)
        {
            if (candidate == anchor.PlayerId) continue;

            var candidateTicket = await _queue.GetTicketAsync(candidate);
            if (candidateTicket == null) continue;

            var trustDiff = Math.Abs(anchor.TrustFactor - candidateTicket.TrustFactor);

            if (trustDiff <= 0.3)
            {
                opponent = candidateTicket;
                break;
            }
        }

        if (opponent != null)
        {
            await CreateMatchAndNotifyAsync(anchor, opponent);
        }
        else
        {
            await _queue.EnqueueAsync(anchor);

            _logger.LogInformation("[Queue] {User} waiting {Wait:N0}s. Delta {Delta} Requeued.",
               anchor.Username, waitTimeSeconds, currentDelta);
            }

    }


    private async Task CreateMatchAndNotifyAsync(MatchmakingTicket p1, MatchmakingTicket p2)
    {
        _queue.RemovePlayer(p1);
        _queue.RemovePlayer(p2);

        var lobbyId = Guid.NewGuid();
        var match = new Match(
            Id: lobbyId,
            Player1Id: p1.PlayerId,
            Player2Id: p2.PlayerId,
            AverageMmr: (p1.Mmr + p2.Mmr) / 2.0,
            CreatedAt: DateTimeOffset.UtcNow
            );

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();
            dbContext.Matches.Add(match);
            await dbContext.SaveChangesAsync();
        }

        var matchPayload = new { LobbyId = lobbyId, AverageMmr = match.AverageMmr };

        await _hubContext.Clients.Group(p1.PlayerId.ToString()).SendAsync("MatchFound", matchPayload);
        await _hubContext.Clients.Group(p2.PlayerId.ToString()).SendAsync("MatchFound", matchPayload);

        _logger.LogWarning("[MATCH FOUND] Lobby {LobbyId}! [{P1}] vs [{P2}] on {Region}!",
            lobbyId, p1.Username, p2.Username, p1.Region);
    }
}
