using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Interfaces.Repositories;

public interface IPartyRepository 
{
    Task<Party?> GetByIdWithMembersAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);

    Task<Party?> GetPartyByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default);

    Task AddAsync(Party party, CancellationToken cancellationToken = default);
    void Update(Party party);
    void Delete(Party party);
}
