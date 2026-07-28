using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using MatchmakingEngine.Domain.Common;
using MatchmakingEngine.Domain.Events;

namespace MatchmakingEngine.Domain;

public class Player : Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
    public ICollection<PartyMember> PartyMemberships { get; set; } = new List<PartyMember>();

    public string PasswordHash { get; set; } = string.Empty;
    public PlayerRole Role { get; set; } = PlayerRole.Player;

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Range(0, 5000)]
    public int Mmr { get; set; } = 1000;

    [Range(0.0, 1.0)]
    public double TrustFactor { get; set; } = 0.5;

    public PlayerRegion Region { get; set; } = PlayerRegion.EuWest;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public void RecordWin(int MmrChange)
    {
        int oldMmr = Mmr;
        Mmr += MmrChange;

        AddDomainEvents(new PlayerMmrChangedEvent(Id, oldMmr, Mmr));
    }
    public void RecordLoss(int MmrChange)
    {
        int oldMmr = Mmr;
        Mmr = Math.Max(0, Mmr - MmrChange);

        AddDomainEvents(new PlayerMmrChangedEvent(Id, oldMmr, Mmr));
    }
}
