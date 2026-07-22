using FluentAssertions;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands;

public class CompleteMatchCommandHandlerTests
{
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CompleteMatchCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateMmrAndFinishMatch()
    {
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var player1 = new Player { Id = player1Id, Username = "Winner", Mmr = 1000 };
        var player2 = new Player { Id = player2Id, Username = "Loser", Mmr = 1000 };

        var match = new Domain.Match(
          Id: Guid.NewGuid(),
          Player1Id: player1Id,
          Player2Id: player2Id,
          AverageMmr: 1000,
          CreatedAt: DateTimeOffset.UtcNow
          )
        { Status = MatchStatus.Accepted };

        match.Player1 = player1;
        match.Player2 = player2;

        _matchRepoMock.Setup(x => x.GetByIdWithPlayersAsync(match.Id, true)).ReturnsAsync(match);


        var handler = new CompleteMatchCommandHandler(_matchRepoMock.Object, _uowMock.Object, _loggerMock.Object);
        var command = new CompleteMatchCommand(match.Id, WinnerId: player1Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MatchStatus.Should().Be(MatchStatus.Finished);
        result.Winner.NewMmr.Should().Be(1025);
        result.Loser.NewMmr.Should().Be(975);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MatchNotAccepted_ShouldThrowConflictException()
    {
          var match = new Domain.Match(
            Id: Guid.NewGuid(),
            Player1Id: Guid.NewGuid(),
            Player2Id: Guid.NewGuid(),
            AverageMmr: 1000,
            CreatedAt: DateTimeOffset.UtcNow
            ) { Status = MatchStatus.Pending };

        _matchRepoMock.Setup(x => x.GetByIdWithPlayersAsync(match.Id, true)).ReturnsAsync(match);

        var handler = new CompleteMatchCommandHandler(_matchRepoMock.Object, _uowMock.Object, _loggerMock.Object);
        var command = new CompleteMatchCommand(match.Id, WinnerId: match.Player1Id);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
