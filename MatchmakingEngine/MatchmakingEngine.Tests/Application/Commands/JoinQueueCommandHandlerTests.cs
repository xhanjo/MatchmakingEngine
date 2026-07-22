using FluentAssertions;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands;

public class JoinQueueCommandHandlerTests
{
    private readonly Mock<IPlayerRepository> _playerRepoMock = new();
    private readonly Mock<IMatchmakingQueue> _queueMock = new();

    [Fact]
    public async Task Handle_ValidPlayer_ShouldAddToQueueAndReturnTrue()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "QueuePlayer",
            Region = PlayerRegion.EuWest,
            Mmr = 1500,
            TrustFactor = 0.5
        };

        _playerRepoMock.Setup(x => x.GetByIdAsync(player.Id, false)).ReturnsAsync(player);
        _queueMock.Setup(x => x.IsPlayerInQueueAsync(player.Id)).ReturnsAsync(false);

        var handler = new JoinQueueCommandHandler(_playerRepoMock.Object, _queueMock.Object);
        var command = new JoinQueueCommand(player.Id);


        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        _queueMock.Verify(q => q.EnqueueAsync(It.Is<MatchmakingTicket>(t =>
       t.PlayerId == player.Id &&
       t.Mmr == player.Mmr
       )), Times.Once);
    }
}
