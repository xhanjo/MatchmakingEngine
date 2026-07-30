
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Infrastructure.Repositories;

public class PartyRepository : IPartyRepository
{
    private readonly MatchmakingDbContext _dbContext;

    public PartyRepository(MatchmakingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Party?> GetByIdWithMembersAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _dbContext.Parties : _dbContext.Parties.AsNoTracking();

        return await query
            .Include(p => p.Members)
            .Include(p => p.Leader)
                .ThenInclude(m => m.MatchPlayers)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Party?> GetPartyByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _dbContext.Parties : _dbContext.Parties.AsNoTracking();

        return await query
            .Include(p => p.Members)
                .ThenInclude(m => m.Player)
            .Include(p => p.Leader)
                .ThenInclude(m => m.MatchPlayers)
                .FirstOrDefaultAsync(p => p.Members.Any(m => m.PlayerId == playerId), cancellationToken);
    }

    public async Task AddAsync(Party party, CancellationToken cancellationToken = default)
    {
        await _dbContext.Parties.AddAsync(party, cancellationToken);
    }

    public void Update(Party party)
    {
        _dbContext.Parties.Update(party);
    }
    public void Delete(Party party)
    {
        _dbContext.Parties.Remove(party);
    }
}
