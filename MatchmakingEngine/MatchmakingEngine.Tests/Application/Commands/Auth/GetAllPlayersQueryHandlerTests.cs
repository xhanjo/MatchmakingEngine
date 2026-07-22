using MatchmakingEngine.Application.Queries.Players;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands.Auth;

public class GetAllPlayersQueryHandlerTests
{
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly GetAllPlayersQueryHandler _handler;

    public GetAllPlayersQueryHandlerTests()
    {
        _playerRepoMock = new Mock<IPlayerRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _handler = new GetAllPlayersQueryHandler(_playerRepoMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsDataWithoutDatabaseCall()
    {
        var cachedPlayers = new List<PlayerResponseDto>
        {
            new PlayerResponseDto(Guid.NewGuid(), "CachedUser", 1000, 0.5, Domain.PlayerRegion.NaEast, Domain.PlayerRole.Player, DateTimeOffset.UtcNow )
        };
        var serializedCache = JsonSerializer.Serialize(cachedPlayers);
        var cachedBytes = Encoding.UTF8.GetBytes(serializedCache);

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        var query = new GetAllPlayersQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("CachedUser", result.First().Username);

        _playerRepoMock.Verify(x => x.GetAllAsync(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_FetchesFromDatabaseAndSetsCache()
    {
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var dbPlayers = new List<Domain.Player>
        {
            new Domain.Player { Id = Guid.NewGuid(), Username = "DbUser", PasswordHash = "hash" }
        };

        _playerRepoMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(dbPlayers);

        var query = new GetAllPlayersQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("DbUser", result.First().Username);

        _cacheMock.Verify(x => x.SetAsync(
         It.IsAny<string>(),
         It.IsAny<byte[]>(),
         It.IsAny<DistributedCacheEntryOptions>(),
         It.IsAny<CancellationToken>()),
         Times.Once);
    }
}