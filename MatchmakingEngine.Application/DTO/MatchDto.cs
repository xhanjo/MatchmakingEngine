using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record MatchPlayerDto(
    Guid PlayerId,
    string Username,
    bool HasAccepted,
    int Kills,
    int Deaths,
    int Assists,
    int Score,
    bool IsMvp,
    int Team,
    bool IsWinner,
    int Mmr = 0,
    int MmrChange = 0
);

public record MatchDto(
    Guid Id,
    MatchStatus Status,
    DateTimeOffset CreatedAt,
    GameMode GameMode,
    string? SelectedMap,
    List<MatchPlayerDto> Players
);
