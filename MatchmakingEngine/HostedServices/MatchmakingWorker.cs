using MatchmakingEngine.Domain;
using MatchmakingEngine.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.SignalR;
using MatchmakingEngine.Hubs;

namespace MatchmakingEngine.Services;

public class MatchmakingWorker : BackgroundService
{
    private readonly IMatchmakingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchmakingWorker> _logger;
    private readonly IHubContext<MatchmakingHub> _hubContext;
    private GameMode _currentModeToProcess = GameMode.Solo;

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
                _currentModeToProcess = _currentModeToProcess == GameMode.Solo ? GameMode.Duo : GameMode.Solo;

                var anchorId = await _queue.DequeueEvaluationIdAsync(_currentModeToProcess, stoppingToken);

                if (anchorId.HasValue)
                    await ProcessEvaluationAsync(anchorId.Value, _currentModeToProcess, stoppingToken);

                await Task.Delay(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured during matchmaking cycle.");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }


    internal async Task ProcessEvaluationAsync(Guid anchorId, GameMode gameMode, CancellationToken cancellationToken)
    {
        var anchor = await _queue.GetTicketAsync(anchorId);
        if (anchor == null) return;

        var waitTimeSeconds = DateTimeOffset.UtcNow - anchor.EnqueuedAt;
        var currentDelta = Math.Min(300, 50 + waitTimeSeconds.TotalSeconds * 5);
        var minMmr = anchor.Mmr - currentDelta;
        var maxMmr = anchor.Mmr + currentDelta;
        var MaxTrustDifference = Math.Min(1.0, 0.3 + (waitTimeSeconds.TotalSeconds / 15.0) * 0.1);

        var candidateIds = await _queue.GetCandidatesByMmrRangeAsync(anchor.Region, gameMode, minMmr, maxMmr);

        MatchmakingTicket? opponent = null;

        foreach (var candidate in candidateIds)
        {
            if (candidate == anchor.PlayerId) continue;

            var candidateTicket = await _queue.GetTicketAsync(candidate);
            if (candidateTicket == null) continue;

            var trustDiff = Math.Abs(anchor.TrustFactor - candidateTicket.TrustFactor);

            if (trustDiff <= MaxTrustDifference)
            {
                opponent = candidateTicket;
                break;
            }
        }

        if (opponent != null)
        {
            await CreateMatchAndNotifyAsync(anchor, opponent, cancellationToken);
        }
        else
        {
            await _queue.EnqueueAsync(anchor);

            _logger.LogInformation("[Queue] [{Mode}] {User} waiting {Wait:N0}s. Delta {Delta} Requeued.",
               gameMode, anchor.Username, waitTimeSeconds.TotalSeconds, currentDelta);
        }

    }

    private async Task CreateMatchAndNotifyAsync(MatchmakingTicket p1, MatchmakingTicket p2, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var matchRepo = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var partyRepo = scope.ServiceProvider.GetRequiredService<IPartyRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var match = new Match(
            Guid.NewGuid(),
            (p1.Mmr + p2.Mmr) / 2,
            DateTimeOffset.UtcNow,
            p1.GameMode
            );

        async Task AddPlayersToTeam(MatchmakingTicket ticket, int teamIndex)
        {
            if (ticket.PartyId.HasValue)
            {
                var party = await partyRepo.GetByIdWithMembersAsync(ticket.PartyId.Value, trackChanges: false, cancellationToken);

                if (party != null)
                {
                    foreach (var member in party.Members)
                    {
                        bool isCap = member.PlayerId == party.LeaderId;
                        match.Players.Add(new MatchPlayer { Id = Guid.NewGuid(), MatchId = match.Id, PlayerId = member.PlayerId, Team = teamIndex, IsCaptain = isCap });
                    }
                }
            }
            else
            {
                match.Players.Add(new MatchPlayer { Id = Guid.NewGuid(), MatchId = match.Id, PlayerId = ticket.PlayerId, Team = teamIndex, IsCaptain = true });
            }
        }

        await AddPlayersToTeam(p1, 1);
        await AddPlayersToTeam(p2, 2);

        await matchRepo.AddAsync(match);
        await uow.SaveChangesAsync(cancellationToken);

        await _queue.RemovePlayerAsync(p1);
        await _queue.RemovePlayerAsync(p2);


        foreach (var player in match.Players)
        {
            await _hubContext.Clients.Group(player.PlayerId.ToString()).SendAsync("MatchFound", match.Id, cancellationToken);
        }

        await _hubContext.Clients.Group("Admins").SendAsync("AdminMatchesUpdated", cancellationToken: cancellationToken);

        _logger.LogInformation("Match {MatchId} created (Mode: {Mode})", match.Id, match.GameMode);
    }
}
