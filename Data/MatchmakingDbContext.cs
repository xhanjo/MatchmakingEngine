using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Domain; 

namespace MatchmakingEngine.Data;

public class MatchmakingDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }

    public MatchmakingDbContext(DbContextOptions<MatchmakingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Username)
            .IsUnique();

        modelBuilder.Entity<Player>()
            .Property(p => p.Region)
            .HasConversion<string>();

        modelBuilder.Entity<Player>()
            .Property(p => p.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Match>()
            .Property(m => m.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player1)
            .WithMany(p => p.MatchesAsPlayer1)
            .HasForeignKey(m => m.Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player2)
            .WithMany(p => p.MatchesAsPlayer2)
            .HasForeignKey(m => m.Player2Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
