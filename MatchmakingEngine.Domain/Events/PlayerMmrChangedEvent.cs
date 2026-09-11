using MatchmakingEngine.Domain.Common;

namespace MatchmakingEngine.Domain.Events;

public class PlayerMmrChangedEvent(Guid playerId, int oldMmr, int newMmr) : IDomainEvent
{
    public Guid PlayerId { get; } = playerId;
    public int OldMmr { get; } = oldMmr;
    public int NewMmr { get; } = newMmr;
}
