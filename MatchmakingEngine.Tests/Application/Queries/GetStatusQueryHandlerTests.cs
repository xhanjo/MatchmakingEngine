using FluentAssertions;
using MatchmakingEngine.Application.Application.Queries.Matchmaking;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using Moq;
using Match = MatchmakingEngine.Domain.Match;

namespace MatchmakingEngine.Tests.Application.Queries;

public class GetStatusQueryHandlerTests
{
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly Mock<IMatchmakingQueue> _queueMock = new();
    private readonly Mock<IPartyRepository> _partyRepoMock = new();

    [Fact]
    public async Task Handle_PlayerInQueue_ShouldReturnSearchingStatus()
    {
        var playerId = Guid.NewGuid();

        _matchRepoMock.Setup(x => x.GetActiveMatchByPlayerIdAsync(playerId, true))
            .ReturnsAsync((Match?)null);

        _queueMock.Setup(q => q.IsPlayerInQueueAsync(playerId))
            .ReturnsAsync(true);

        var handler = new GetStatusQueryHandler(_matchRepoMock.Object, _queueMock.Object, _partyRepoMock.Object);
        var query = new GetStatusQuery(playerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(nameof(PollingStatus.Searching));
    }

    [Fact]
    public async Task Handle_PlayerNotInQueueAndNoMatch_ShouldReturnIdle()
    {
        var playerId = Guid.NewGuid();

        _matchRepoMock.Setup(x => x.GetActiveMatchByPlayerIdAsync(playerId, true))
            .ReturnsAsync((Match?)null);

        _queueMock.Setup(q => q.IsPlayerInQueueAsync(playerId))
            .ReturnsAsync(false);
            
        _partyRepoMock.Setup(p => p.GetPartyByPlayerIdAsync(playerId, false, CancellationToken.None))
            .ReturnsAsync((Party?)null);

        var handler = new GetStatusQueryHandler(_matchRepoMock.Object, _queueMock.Object, _partyRepoMock.Object);
        var query = new GetStatusQuery(playerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be(nameof(PollingStatus.Idle));
    }
}
