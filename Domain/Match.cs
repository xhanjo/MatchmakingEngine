namespace MatchmakingEngine.Domain;

public record Match(
    Guid Id,
    Guid Player1Id,
    Guid Player2Id,
    double AverageMmr,
    DateTimeOffset CreatedAt
)
{
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public Player Player1 { get; set; } = null!;
    public Player Player2 { get; set; } = null!;
    public bool Player1Accepted { get; set; } = false;
    public bool Player2Accepted { get; set; } = false;
}