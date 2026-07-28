namespace MatchmakingEngine.Domain;

public class Friendship
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Player Sender { get; set; }
    public Player Receiver { get; set; }
}
