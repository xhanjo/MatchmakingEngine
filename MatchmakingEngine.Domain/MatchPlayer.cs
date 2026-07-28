namespace MatchmakingEngine.Domain;

public class MatchPlayer
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public int Team { get; set; }
    public bool Accepted { get; set; } = false;
    public int Kills { get; set; } = 0;
    public int Deaths { get; set; } = 0;
    public int Assists { get; set; } = 0;
    public int Score { get; set; } = 0;
    public bool IsMvp { get; set; } = false;
    public Match Match { get; set; }
    public Player Player { get; set; }
}
