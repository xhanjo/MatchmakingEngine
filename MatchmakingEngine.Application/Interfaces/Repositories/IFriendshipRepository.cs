using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Interfaces.Repositories;

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default);
    Task<Friendship?> GetBetweenPlayersAsync(Guid player1Id, Guid player2Id, CancellationToken ct = default);
    Task<IEnumerable<Friendship>> GetFriendsAsync(Guid playerId, CancellationToken ct = default);
    Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid playerId, CancellationToken ct = default);
    Task AddAsync(Friendship friendship, CancellationToken ct = default);
    void Update(Friendship friendship);
    void Delete(Friendship friendship);
}
