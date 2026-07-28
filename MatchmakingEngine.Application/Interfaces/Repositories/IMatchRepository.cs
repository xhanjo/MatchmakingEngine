using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Interfaces.Repositories;

public interface IMatchRepository
{
    Task<Match?> GetByIdWithPlayersAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<Match?> GetActiveMatchByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<Match>> GetMatchHistoryByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<Match>> GetAllMatchesAsync(bool trackChanges = false, CancellationToken cancellationToken = default);
    Task AddAsync(Match match, CancellationToken cancellationToken = default);
    void Update(Match match);
}