using MatchmakingEngine.Domain;
using MatchmakingEngine.Infrastructure.Repositories;
using MatchmakingEngine.Tests.Helpers;

namespace MatchmakingEngine.Tests.Integration;

public class PlayerRepositoryIntegrationTests : BaseIntegrationTest
{
    public PlayerRepositoryIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AddAsync_ShouldSavePlayerToRealDatabase()
    {
        var context = GetDbContext();
        
        // Створюємо схему БД у нашому порожньому Docker-контейнері
        await context.Database.EnsureCreatedAsync(); 
        
        var repository = new PlayerRepository(context);
        
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "DockerUser123",
            PasswordHash = "hash",
            Role = PlayerRole.Player,
            Region = PlayerRegion.EuWest
        };

        await repository.AddAsync(player);
        await context.SaveChangesAsync();

        var savedPlayer = await repository.GetByUsernameAsync("DockerUser123", false);
        
        Assert.NotNull(savedPlayer);
        Assert.Equal("DockerUser123", savedPlayer.Username);
    }
}