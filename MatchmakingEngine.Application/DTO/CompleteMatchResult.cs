using MatchmakingEngine.Domain;

namespace MatchmakingEngine.DTO;

public record CompleteMatchResult(
    string Message,
    PlayerMatchResultDto Winner,
    PlayerMatchResultDto Loser,
    MatchStatus MatchStatus
    );

public record PlayerMatchResultDto(
    string Username, 
    double NewMmr
    );
