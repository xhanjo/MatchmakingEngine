using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record AcceptMatchResult(
    string StatusMessage,
    bool AllAccepted,
    MatchStatus MatchStatus,
    List<Guid> PlayerIds,
    MatchmakingEngine.Application.DTO.VetoStateDto? VetoState = null
    );