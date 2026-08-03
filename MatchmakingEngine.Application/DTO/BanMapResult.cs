using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record BanMapResult(
    List<Guid> PlayerIds, 
    string BannedMap, 
    List<string> AvailableMaps,
    Guid? NextTurnPlayerId,
    MatchStatus Status
);
