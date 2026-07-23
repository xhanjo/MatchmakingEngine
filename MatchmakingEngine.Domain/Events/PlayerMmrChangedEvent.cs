using MatchmakingEngine.Domain.Common;

namespace MatchmakingEngine.Domain.Events;

public class PlayerMmrChangedEvent : IDomainEvent
{
    public Guid PlayerId { get; }
    public int OldMmr { get; }
    public int NewMmr { get; }

    public PlayerMmrChangedEvent(Guid playerId, int oldMmr, int newMmr)
    {
        PlayerId = playerId;
        OldMmr = oldMmr;
        NewMmr = newMmr;
    }
}
