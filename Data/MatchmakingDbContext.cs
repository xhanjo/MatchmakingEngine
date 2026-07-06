using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Domain; 

namespace MatchmakingEngine.Data;

public class MatchmakingDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }

    public MatchmakingDbContext(DbContextOptions<MatchmakingDbContext> options) : base(options)
    {        
    }
}
