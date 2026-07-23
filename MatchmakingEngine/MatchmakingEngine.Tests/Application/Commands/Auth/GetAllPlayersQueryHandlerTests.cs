using MatchmakingEngine.Application.Queries.Players;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Application.Interfaces;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands.Auth;

public class GetAllPlayersQueryHandlerTests
{
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetAllPlayersQueryHandler _handler;

    public GetAllPlayersQueryHandlerTests()
    {
        _playerRepoMock = new Mock<IPlayerRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetAllPlayersQueryHandler(_playerRepoMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_ExecutesFactory_ReturnsMappedPlayers()
    {
        var dbPlayers = new List<Domain.Player>
        {
            new Domain.Player { Id = Guid.NewGuid(), Username = "TestUser", PasswordHash = "hash"}
        };
        _playerRepoMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(dbPlayers);

        _cacheMock.Setup(x => x.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<List<PlayerResponseDto>>>>(),
            It.IsAny<TimeSpan?>()))
            .Returns(async (string key, Func<Task<List<PlayerResponseDto>>> factory, TimeSpan? expiration) =>
            {
                return await factory();
            });

        var query = new GetAllPlayersQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("TestUser", result.First().Username);

        _playerRepoMock.Verify(x => x.GetAllAsync(false), Times.Once);
    }
}