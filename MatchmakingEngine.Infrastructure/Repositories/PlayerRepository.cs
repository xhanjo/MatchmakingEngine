using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly MatchmakingDbContext _context;
    public PlayerRepository(MatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Player>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Players : _context.Players.AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Player?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Players : _context.Players.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Player?> GetByUsernameAsync(string username, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Players : _context.Players.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
    }

    public async Task<IEnumerable<Player>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Players : _context.Players.AsNoTracking();
        return await query.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        await _context.Players.AddAsync(player, cancellationToken);
    }

    public async Task<Player?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .Include(p => p.RefreshTokens)
            .FirstOrDefaultAsync(p => p.RefreshTokens.Any(rt => rt.Token == refreshToken), cancellationToken);
    }

    public void Update(Player player)
    {
        _context.Players.Update(player);
    }
}
