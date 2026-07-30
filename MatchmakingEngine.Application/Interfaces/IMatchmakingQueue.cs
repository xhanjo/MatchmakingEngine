using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Services;

public interface IMatchmakingQueue
{
    ValueTask EnqueueAsync(MatchmakingTicket ticket);
    ValueTask<Guid?> DequeueEvaluationIdAsync(GameMode gameMode, CancellationToken cancellationToken = default);

    ValueTask<MatchmakingTicket?> GetTicketAsync(Guid playerId);

    ValueTask<Guid[]> GetCandidatesByMmrRangeAsync(PlayerRegion region,GameMode gameMode, double minMmr, double maxMmr);

    ValueTask<bool> IsPlayerInQueueAsync(Guid playerId);
    ValueTask RemovePlayerAsync(MatchmakingTicket ticket);
}
