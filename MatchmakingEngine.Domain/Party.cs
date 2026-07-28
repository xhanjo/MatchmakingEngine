namespace MatchmakingEngine.Domain;

public class Party
{
    public Guid Id { get; set; }
    public Guid LeaderId { get; set; }
    public GameMode GameMode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } 
    public Player Leader { get; set; } 
    public ICollection<PartyMember> Members { get; set; }
}
