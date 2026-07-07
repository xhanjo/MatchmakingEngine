using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Services;

public interface IMatchmakingQueue
{
    ValueTask EnqueueAsync(MatchmakingTicket ticket);
    ValueTask<MatchmakingTicket?> DequeueAsync(CancellationToken cancellationToken);

    bool IsPlayerInQueue(Guid playerId);
    void RemovePlayer(Guid playerId);
}
