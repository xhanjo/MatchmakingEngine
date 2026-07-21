using MatchmakingEngine.Application.Queries.Players;
using MatchmakingEngine.Data;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MatchmakingEngine.Tests.Application.Commands.Auth;

public class GetAllPlayersQueryHandlerTests : IDisposable
{
    private readonly MatchmakingDbContext _context;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly GetAllPlayersQueryHandler _handler;

    public GetAllPlayersQueryHandlerTests()
    {
        _context = TestDbContextFactory.Create();
        _cacheMock = new Mock<IDistributedCache>();
        _handler = new GetAllPlayersQueryHandler(_context, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsDataWithoutDatabaseCall()
    {
        var cachedPlayers = new List<PlayerResponseDto>
        {
            new PlayerResponseDto(Guid.NewGuid(), "CachedUser", 1000.0, 0.5, Domain.PlayerRegion.NaEast, Domain.PlayerRole.Player, DateTimeOffset.UtcNow )
        };
        var serializedCache = JsonSerializer.Serialize(cachedPlayers);
        var cachedBytes = Encoding.UTF8.GetBytes(serializedCache);

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        var query = new GetAllPlayersQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("CachedUser", result.First().Username);
        Assert.Empty(_context.Players);
    }

    [Fact]
    public async Task Handle_CacheMiss_FetchesFromDatabaseAndSetsCache()
    {
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _context.Players.Add(new Domain.Player { Id =  Guid.NewGuid(), Username = "DbUser", PasswordHash = "hash"});
        await _context.SaveChangesAsync();

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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
