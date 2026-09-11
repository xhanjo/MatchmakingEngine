namespace MatchmakingEngine.Application.DTO;

public record PollingStatusResponseDto(
    string Status,
    Guid? LobbyId = null,
    double? AverageMmr = null,
    DateTimeOffset? CreatedAt = null,
    MatchmakingEngine.Application.DTO.VetoStateDto? VetoState = null
    );