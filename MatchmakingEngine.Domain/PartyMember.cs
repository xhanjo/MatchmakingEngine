namespace MatchmakingEngine.Domain;

public class PartyMember
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid PlayerId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public Party Party { get; set; }
    public Player Player { get; set; }
}
