using MatchmakingEngine.Domain;

namespace MatchmakingEngine.DTO;

public record AcceptMatchResult(
    string StatusMessage,
    bool Player1Accepted,
    bool Player2Accepted,
    MatchStatus MatchStatus
    );