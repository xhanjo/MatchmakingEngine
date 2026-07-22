using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Interfaces.Repositories;

public interface IMatchRepository
{
    Task<Match?> GetByIdWithPlayersAsync(Guid id, bool trackChanges = false);
    Task<Match?> GetActiveMatchByPlayerIdAsync(Guid playerId, bool trackChanges = false);
    Task<IEnumerable<Match>> GetMatchHistoryByPlayerIdAsync(Guid playerId, bool trackChanges = false);
    Task AddAsync(Match match);
    void Update(Match match);
}
