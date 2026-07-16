using FluentAssertions;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands;

public class CompleteMatchCommandHandlerTests
{
    private readonly Mock<ILogger<CompleteMatchCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateMmrAndFinishMatch()
    {
        using var context = TestDbContextFactory.Create();

        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var player1 = new Player { Id = player1Id, Username = "Winner", Mmr = 1000 };
        var player2 = new Player { Id = player2Id, Username = "Loser", Mmr = 1000 };

        context.Players.AddRange(player1, player2);

        var match = new Domain.Match(
            Id: Guid.NewGuid(),
            Player1Id: player1Id,
            Player2Id: player2Id,
            AverageMmr: 1000,
            CreatedAt: DateTimeOffset.UtcNow
            ) { Status = MatchStatus.Accepted };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var handler = new CompleteMatchCommandHandler(context, _loggerMock.Object);
        var command = new CompleteMatchCommand(match.Id, WinnerId: player1Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MatchStatus.Should().Be(MatchStatus.Finished);
        result.Winner.NewMmr.Should().Be(1025.0);
        result.Loser.NewMmr.Should().Be(975.0);
    }

    [Fact]
    public async Task Handle_MatchNotAccepted_ShouldThrowConflictException()
    {
        using var context = TestDbContextFactory.Create();

        var match = new Domain.Match(
            Id: Guid.NewGuid(),
            Player1Id: Guid.NewGuid(),
            Player2Id: Guid.NewGuid(),
            AverageMmr: 1000,
            CreatedAt: DateTimeOffset.UtcNow
            ) { Status = MatchStatus.Pending };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var handler = new CompleteMatchCommandHandler(context, _loggerMock.Object);
        var command = new CompleteMatchCommand(match.Id, WinnerId: match.Player1Id);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
