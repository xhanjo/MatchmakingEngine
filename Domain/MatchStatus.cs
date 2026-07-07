namespace MatchmakingEngine.Domain;

public enum MatchStatus
{
    Pending, // Waiting for all players to accept match
    Accepted, // All players accept match
    Canceled // Somebody reject or don't accept match
}
