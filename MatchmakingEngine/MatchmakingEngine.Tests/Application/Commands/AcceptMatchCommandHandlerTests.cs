using FluentAssertions;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands;

public class AcceptMatchCommandHandlerTests
{
    private readonly Mock<ILogger<AcceptMatchCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_Player1Accepts_ShouldStayPending()
    {
        using var context = TestDbContextFactory.Create();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var match = new Domain.Match(Guid.NewGuid(), p1, p2, 1000, DateTimeOffset.UtcNow);
        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var handler = new AcceptMatchCommandHandler(context, _loggerMock.Object);
        var command = new AcceptMatchCommand(p1, match.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MatchStatus.Should().Be(MatchStatus.Pending);
        result.Player1Accepted.Should().BeTrue();
        result.Player2Accepted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonParticipant_ShouldThrowConflictException()
    {
        using var context = TestDbContextFactory.Create();
        var match = new Domain.Match(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, DateTimeOffset.UtcNow);
        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var handler = new AcceptMatchCommandHandler(context, _loggerMock.Object);
        var strangerPlayerId = Guid.NewGuid();

        var act = () => handler.Handle(new AcceptMatchCommand(strangerPlayerId, match.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*not a participant*");
    }

    [Fact]
    public async Task Handle_BothPlayersAccept_ShouldBecomeAccepted()
    {
        using var context = TestDbContextFactory.Create();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var match = new Domain.Match(Guid.NewGuid(), p1, p2, 1000, DateTimeOffset.UtcNow)
        {
            Player1Accepted = true,
            Status = MatchStatus.Pending
        };
        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var handler = new AcceptMatchCommandHandler(context, _loggerMock.Object);
        var command = new AcceptMatchCommand(p2, match.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MatchStatus.Should().Be(MatchStatus.Accepted);
        result.Player2Accepted.Should().BeTrue();


    }
}
