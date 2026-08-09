using System.ComponentModel.DataAnnotations;

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
    public List<string> AvailableMaps { get; set; } = new();
    public List<string> BannedMaps { get; set; } = new();
    public string? SelectedMap { get; set; }
    public Guid? CurrentVetoTurnPlayerId { get; set; }
    public DateTimeOffset? VetoDeadLine { get; set; }
    [Timestamp]
    public uint Version { get; set; }
}