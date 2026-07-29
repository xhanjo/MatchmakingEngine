
namespace MatchmakingEngine.Application.DTO;

public record FriendRequestDto(Guid RequestId, Guid SenderId, string SenderUsername, DateTimeOffset CreatedAt);
