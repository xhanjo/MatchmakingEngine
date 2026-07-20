using FluentAssertions;
using MatchmakingEngine.Application.Queries.Matchmaking;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using MatchmakingEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Queries;

public class GetStatusQueryHandlerTests
{
    private readonly Mock<IMatchmakingQueue> _queueMock = new();

    [Fact]
    public async Task Handle_PlayerInQueue_ShouldReturnSearchingStatus()
    {
        using var context = TestDbContextFactory.Create();
        var playerId = Guid.NewGuid();

        var player = new Player { Id = playerId , Username = "TestPlayer", Mmr = 1000};
        context.Players.Add(player);
        await context.SaveChangesAsync();

        _queueMock.Setup(q => q.IsPlayerInQueueAsync(playerId))
            .Returns(ValueTask.FromResult(true));

        var handler = new GetStatusQueryHandler(context, _queueMock.Object);
        var query = new GetStatusQuery(playerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(PollingStatus.Searching.ToString());
    }

    [Fact]
    public async Task Handle_PlayerNotInQueueAndNoMatch_ShouldReturnIdle()
    {
        using var context = TestDbContextFactory.Create();
        var playerId = Guid.NewGuid();

        var player = new Player { Id = playerId, Username = "TestPlayer", Mmr = 1000 };
        context.Players.Add(player);
        await context.SaveChangesAsync();

        _queueMock.Setup(q => q.IsPlayerInQueueAsync(playerId))
            .Returns(ValueTask.FromResult(false));

        var handler = new GetStatusQueryHandler(context, _queueMock.Object);
        var query = new GetStatusQuery(playerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(PollingStatus.Idle.ToString());
    }
}
