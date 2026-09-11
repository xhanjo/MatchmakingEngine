using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record PlayerResponseDto(
    Guid Id,
    string Username,
    int Mmr,
    double TrustFactor,
    PlayerRegion Region,
    PlayerRole Role,
    DateTimeOffset CreatedAt
    );
