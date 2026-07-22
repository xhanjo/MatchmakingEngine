using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Application.Interfaces;

namespace MatchmakingEngine.Infrastructure.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly IMatchmakingDbContext _context;
    public MatchRepository(IMatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task<Match?> GetByIdWithPlayersAsync(Guid id, bool trackChanges = false)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Match?> GetActiveMatchByPlayerIdAsync(Guid playerId, bool trackChanges = false)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m =>
            (m.Player1Id == playerId || m.Player2Id == playerId) &&
            (m.Status == MatchStatus.Pending || m.Status == MatchStatus.Accepted));
    }

    public async Task<IEnumerable<Match>> GetMatchHistoryByPlayerIdAsync(Guid playerId, bool trackChanges = false)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
            .ToListAsync();
    }

    public void Update(Match match)
    {
        _context.Matches.Update(match);
    }

    public async Task AddAsync(Match match)
    {
        await _context.Matches.AddAsync(match);
    }
}
