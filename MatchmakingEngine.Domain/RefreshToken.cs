namespace MatchmakingEngine.Domain;

public class RefreshToken
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    
    public DateTime? RevokedAt { get; set; }

    public Guid PlayerId { get; set; }
    
    public Player Player { get; set; } = null!;

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
