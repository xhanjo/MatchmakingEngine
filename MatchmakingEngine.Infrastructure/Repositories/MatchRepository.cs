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
            .Include(m => m.Players)
            .ThenInclude(mp => mp.Player)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Match?> GetActiveMatchByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .FirstOrDefaultAsync(m =>
                m.Players.Any(mp => mp.PlayerId == playerId) &&
                (m.Status == MatchStatus.Pending || m.Status == MatchStatus.Accepted), cancellationToken);
    }

    public async Task<IEnumerable<Match>> GetMatchHistoryByPlayerIdAsync(Guid playerId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Players)
            .ThenInclude(mp => mp.Player)
            .Where(m => m.Players.Any(mp => mp.PlayerId == playerId))
            .ToListAsync(cancellationToken);
    }
    public async Task<IEnumerable<Match>> GetAllMatchesAsync(bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Matches : _context.Matches.AsNoTracking();

        return await query
            .Include(m => m.Players)
            .ThenInclude(mp => mp.Player)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<Match>> GetMatchesInVetoTimeoutAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .Include(m => m.Players)
            .Where(m => m.Status == MatchStatus.MapVeto && m.VetoDeadLine < currentTime)
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
