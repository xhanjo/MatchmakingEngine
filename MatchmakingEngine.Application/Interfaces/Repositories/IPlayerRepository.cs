using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Interfaces.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<Player?> GetByUsernameAsync(string username, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<Player>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<Player>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task AddAsync(Player player, CancellationToken cancellationToken = default);
    Task<Player?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    void Update(Player player);
}
