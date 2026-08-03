namespace MatchmakingEngine.Domain;

public enum MatchStatus
{
    Pending,
    Accepted,
    MapVeto,
    StartingServer,
    Live,
    Canceled,
    Finished
}
