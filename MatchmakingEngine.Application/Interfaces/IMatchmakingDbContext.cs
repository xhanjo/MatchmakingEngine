using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MatchmakingEngine.Application.Interfaces;

public interface IMatchmakingDbContext
{
    DbSet<Player> Players { get; set; }
    DbSet<Match> Matches { get; set; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
