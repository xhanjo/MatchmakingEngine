using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record LeaderboardEntryDto(
    long Rank,
    Guid PlayerId,
    string Username,
    int Mmr,
    PlayerRegion Region
);
