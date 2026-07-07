namespace MatchmakingEngine.DTO;

public record PollingStatusResponseDto(
    string Status,
    Guid? LobbyId = null,
    double? AverageMmr = null,
    DateTimeOffset? CreatedAt = null
    );