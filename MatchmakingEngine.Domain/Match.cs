namespace MatchmakingEngine.Domain;

public record Match(
    Guid Id,
    int AverageMmr,
    DateTimeOffset CreatedAt,
    GameMode GameMode
)
{
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public ICollection<MatchPlayer> Players { get; set; } = new List<MatchPlayer>();
}