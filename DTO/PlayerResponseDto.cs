using MatchmakingEngine.Domain;

namespace MatchmakingEngine.DTO;

public record PlayerResponseDto(
    Guid Id,
    string Username,
    double Mmr,
    double TrustFactor,
    PlayerRegion Region,
    PlayerRole Role,
    DateTimeOffset CreatedAt
    );
