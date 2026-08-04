using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record BanMapResult(
    List<Guid> PlayerIds, 
    VetoStateDto VetoState
);
