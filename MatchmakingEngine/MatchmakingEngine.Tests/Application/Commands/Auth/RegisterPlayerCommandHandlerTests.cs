using Moq;
using MatchmakingEngine.Application.Commands.Players;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands.Players;

public class RegisterPlayerCommandHandlerTests
{
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly RegisterPlayerCommandHandler _handler;
    private readonly Mock<ILeaderboardService> _leaderboardService;

    public RegisterPlayerCommandHandlerTests()
    {
        _playerRepoMock = new Mock<IPlayerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<ICacheService>();
        _leaderboardService = new Mock<ILeaderboardService>();

        _handler = new RegisterPlayerCommandHandler(
            _playerRepoMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object,
            _leaderboardService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesPlayerAndInvalidatesCache()
    {
        var command = new RegisterPlayerCommand("NewUser", "Password123", Domain.PlayerRegion.EuWest);

        _playerRepoMock
            .Setup(repo => repo.GetByUsernameAsync(command.Username, false))
            .ReturnsAsync((Player?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(command.Username, result.Username);

        _playerRepoMock.Verify(repo => repo.AddAsync(It.Is<Player>(p => p.Username == command.Username)), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Перевірка інвалідації кешу після реєстрації
        _cacheMock.Verify(c => c.RemoveAsync("all_players", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingUsername_ThrowsException()
    {
        var command = new RegisterPlayerCommand("ExistingUser", "Password123", Domain.PlayerRegion.EuEast);

        _playerRepoMock
            .Setup(repo => repo.GetByUsernameAsync(command.Username, false))
            .ReturnsAsync(new Player { Username = "ExistingUser" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _playerRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Player>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}