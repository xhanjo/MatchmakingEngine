using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Data;

namespace MatchmakingEngine.Infrastructure.Repositories;


public class UnitOfWork : IUnitOfWork
{
    private readonly MatchmakingDbContext _context;
    public UnitOfWork(MatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
