using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record CompleteMatchResult(
    string Message,
    int WinningTeam,
    Guid MvpPlayerId,
    string MvpUsername,
    List<PlayerStatsDto> Scoreboard,
    MatchStatus Status
    );

public record PlayerStatsDto(
    Guid PlayerId,
    string Username,
    int Team,
    int Kills,
    int Deaths,
    int Assists,
    int Score,
    bool IsMvp,
    int MmrChange
    );
