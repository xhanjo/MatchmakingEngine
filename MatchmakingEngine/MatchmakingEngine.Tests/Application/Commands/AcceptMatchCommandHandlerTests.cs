using FluentAssertions;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands;

public class AcceptMatchCommandHandlerTests
{
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<AcceptMatchCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_Player1Accepts_ShouldStayPending()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var match = new Domain.Match(Guid.NewGuid(), p1, p2, 1000, DateTimeOffset.UtcNow);

        _matchRepoMock.Setup(x => x.GetByIdWithPlayersAsync(match.Id, true)).ReturnsAsync(match);


        var handler = new AcceptMatchCommandHandler(_matchRepoMock.Object, _uowMock.Object, _loggerMock.Object);
        var command = new AcceptMatchCommand(p1, match.Id);


        var result = await handler.Handle(command, CancellationToken.None);

        result.MatchStatus.Should().Be(MatchStatus.Pending);
        result.Player1Accepted.Should().BeTrue();
        result.Player2Accepted.Should().BeFalse();

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonParticipant_ShouldThrowConflictException()
    {
        var match = new Domain.Match(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, DateTimeOffset.UtcNow);
        _matchRepoMock.Setup(x => x.GetByIdWithPlayersAsync(match.Id, true)).ReturnsAsync(match);


        var handler = new AcceptMatchCommandHandler(_matchRepoMock.Object, _uowMock.Object, _loggerMock.Object);
        var strangerPlayerId = Guid.NewGuid();

        var act = () => handler.Handle(new AcceptMatchCommand(strangerPlayerId, match.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*not a participant*");
    }

    [Fact]
    public async Task Handle_BothPlayersAccept_ShouldBecomeAccepted()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var match = new Domain.Match(Guid.NewGuid(), p1, p2, 1000, DateTimeOffset.UtcNow)
        {
            Player1Accepted = true,
            Status = MatchStatus.Pending
        };
        _matchRepoMock.Setup(x => x.GetByIdWithPlayersAsync(match.Id, true)).ReturnsAsync(match);

        var handler = new AcceptMatchCommandHandler(_matchRepoMock.Object, _uowMock.Object, _loggerMock.Object);
        var command = new AcceptMatchCommand(p2, match.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MatchStatus.Should().Be(MatchStatus.Accepted);
        result.Player2Accepted.Should().BeTrue();

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
