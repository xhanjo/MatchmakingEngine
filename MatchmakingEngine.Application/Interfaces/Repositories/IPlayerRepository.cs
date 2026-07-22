using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Interfaces.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id, bool trackChanges = false);
    Task<Player?> GetByUsernameAsync(string username, bool trackChanges = false);
    Task<IEnumerable<Player>> GetAllAsync(bool trackChanges = false);
    Task AddAsync(Player player);
    void Update(Player player);
}
