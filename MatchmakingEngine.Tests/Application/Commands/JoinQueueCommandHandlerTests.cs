using FluentAssertions;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands;

public class JoinQueueCommandHandlerTests
{
    private readonly Mock<IMatchmakingQueue> _queueMock = new();

    [Fact]
    public async Task Handle_ValidPlayer_ShouldAddToQueueAndReturnTrue()
    {
        using var context = TestDbContextFactory.Create();

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "QueuePlayer",
            Region = PlayerRegion.EuWest,
            Mmr = 1500,
            TrustFactor = 0.5
        };
        context.Players.Add(player);
        await context.SaveChangesAsync();

        var handler = new JoinQueueCommandHandler(context, _queueMock.Object);
        var command = new JoinQueueCommand(player.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        _queueMock.Verify(q => q.EnqueueAsync(It.Is<MatchmakingTicket>(t =>
        t.PlayerId == player.Id &&
        t.Mmr == player.Mmr
        )), Times.Once);
    }
}
