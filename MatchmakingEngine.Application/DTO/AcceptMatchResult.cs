using MatchmakingEngine.Domain;

namespace MatchmakingEngine.DTO;

public record AcceptMatchResult(
    string StatusMessage,
    bool AllAccepted,
    MatchStatus MatchStatus,
    List<Guid> PlayerIds,
    MatchmakingEngine.Application.DTO.VetoStateDto? VetoState = null
    );