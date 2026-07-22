using FluentAssertions;
using MatchmakingEngine.Application.Queries.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Queries;

public class GetStatusQueryHandlerTests
{
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly Mock<IMatchmakingQueue> _queueMock = new();

    [Fact]
    public async Task Handle_PlayerInQueue_ShouldReturnSearchingStatus()
    {
        var playerId = Guid.NewGuid();

        _matchRepoMock.Setup(x => x.GetActiveMatchByPlayerIdAsync(playerId, false))
            .ReturnsAsync((Domain.Match?)null);


        _queueMock.Setup(q => q.IsPlayerInQueueAsync(playerId))
            .ReturnsAsync(true);

        var handler = new GetStatusQueryHandler(_matchRepoMock.Object, _queueMock.Object);
        var query = new GetStatusQuery(playerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(PollingStatus.Searching.ToString());
    }

    [Fact]
    public async Task Handle_PlayerNotInQueueAndNoMatch_ShouldReturnIdle()
    {
        var playerId = Guid.NewGuid();

        _matchRepoMock.Setup(x => x.GetActiveMatchByPlayerIdAsync(playerId, false))
            .ReturnsAsync((Domain.Match?)null);


        _queueMock.Setup(q => q.IsPlayerInQueueAsync(playerId))
            .ReturnsAsync(false);

        var handler = new GetStatusQueryHandler(_matchRepoMock.Object, _queueMock.Object);
        var query = new GetStatusQuery(playerId);


        var result = await handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(PollingStatus.Idle.ToString());
    }
}
