using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Application.Interfaces;

namespace MatchmakingEngine.Infrastructure.Repositories;


public class UnitOfWork : IUnitOfWork
{
    private readonly IMatchmakingDbContext _context;

    public UnitOfWork(IMatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
