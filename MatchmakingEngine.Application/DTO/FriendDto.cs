
using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record FriendDto(Guid FriendshipId, Guid PlayerId, string Username, int Mmr, PlayerRegion Region);
