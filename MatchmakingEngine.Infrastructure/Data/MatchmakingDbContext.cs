using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Application.Interfaces;

namespace MatchmakingEngine.Data;

public class MatchmakingDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchPlayer> MatchPlayers { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Party> Parties { get; set; }
    public DbSet<PartyMember> PartyMembers { get; set; }

    public MatchmakingDbContext(DbContextOptions<MatchmakingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== Player =====
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Username)
            .IsUnique();

        modelBuilder.Entity<Player>()
            .Property(p => p.Region)
            .HasConversion<string>();

        modelBuilder.Entity<Player>()
            .Property(p => p.Role)
            .HasConversion<string>();

        // ===== Match =====
        modelBuilder.Entity<Match>()
            .Property(m => m.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Match>()
            .Property(m => m.Version)
            .IsRowVersion();

        // ===== MatchPlayer =====
        modelBuilder.Entity<MatchPlayer>()
            .HasOne(mp => mp.Match)
            .WithMany(m => m.Players)
            .HasForeignKey(mp => mp.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatchPlayer>()
            .HasOne(mp => mp.Player)
            .WithMany(p => p.MatchPlayers)
            .HasForeignKey(mp => mp.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MatchPlayer>()
            .HasIndex(mp => mp.MatchId);

        modelBuilder.Entity<MatchPlayer>()
            .HasIndex(mp => mp.PlayerId);

        // ===== Friendship =====
        modelBuilder.Entity<Friendship>()
            .Property(f => f.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Sender)
            .WithMany(p => p.SentFriendRequests)
            .HasForeignKey(f => f.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Receiver)
            .WithMany(p => p.ReceivedFriendRequests)
            .HasForeignKey(f => f.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasIndex(f => new { f.SenderId, f.ReceiverId })
            .IsUnique();

        // ===== Party =====
        modelBuilder.Entity<Party>()
            .Property(p => p.GameMode)
            .HasConversion<string>();

        modelBuilder.Entity<Party>()
            .HasOne(p => p.Leader)
            .WithMany()
            .HasForeignKey(p => p.LeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== PartyMember =====
        modelBuilder.Entity<PartyMember>()
            .HasOne(pm => pm.Party)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.PartyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PartyMember>()
            .HasOne(pm => pm.Player)
            .WithMany(p => p.PartyMemberships)
            .HasForeignKey(pm => pm.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
