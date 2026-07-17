using System.ComponentModel.DataAnnotations;

namespace MatchmakingEngine.Domain;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ICollection<Match> MatchesAsPlayer1 { get; set; } = new List<Match>();
    public ICollection<Match> MatchesAsPlayer2 { get; set; } = new List<Match>();

    public string PasswordHash { get; set; } = string.Empty;
    public PlayerRole Role { get; set; } = PlayerRole.Player;

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Range(0, 5000)]
    public double Mmr { get; set; } = 1000;

    [Range(0.0, 1.0)]
    public double TrustFactor { get; set; } = 0.5;

    public PlayerRegion Region { get; set; } = PlayerRegion.EuWest;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public void RecordWin(double mmrChange)
    {
        Mmr += mmrChange;
    }
    public void RecordLoss(double mmrChange)
    {
        Mmr = Math.Max(0, Mmr - mmrChange);
    }
}
