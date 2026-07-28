using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Infrastructure.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly MatchmakingDbContext _context;
    public MatchRepository(MatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task<Match?> GetByIdWithPlayersAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Match?> GetActiveMatchByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m =>
            (m.Player1Id == playerId || m.Player2Id == playerId) &&
            (m.Status == MatchStatus.Pending || m.Status == MatchStatus.Accepted), cancellationToken);
    }

    public async Task<IEnumerable<Match>> GetMatchHistoryByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
            .ToListAsync(cancellationToken);
    }
    public async Task<IEnumerable<Match>> GetAllMatchesAsync(bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Match match, CancellationToken cancellationToken = default)
    {
        await _context.Matches.AddAsync(match, cancellationToken);
    }
    public void Update(Match match)
    {
        _context.Matches.Update(match);
    }
}
