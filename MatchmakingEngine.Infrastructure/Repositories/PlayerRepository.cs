using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly MatchmakingDbContext _context;
    public PlayerRepository(MatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Player player)
    {
        await _context.Players.AddAsync(player);
    }

    public async Task<IEnumerable<Player>> GetAllAsync(bool trackChanges = false)
    {
        var query = trackChanges ? _context.Players : _context.Players.AsNoTracking();
        return await query.ToListAsync();
    }

    public async Task<Player?> GetByIdAsync(Guid id, bool trackChanges = false)
    {
        var query = trackChanges ? _context.Players : _context.Players.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Player?> GetByUsernameAsync(string username, bool trackChanges = false)
    {
        var query = trackChanges ? _context.Players : _context.Players.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Username == username);
    }

    public void Update(Player player)
    {
        _context.Players.Update(player);
    }
}
