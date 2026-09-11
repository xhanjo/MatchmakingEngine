using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Match = MatchmakingEngine.Domain.Match;
using MatchmakingEngine.Application.Application.Commands.Matchmaking;

namespace MatchmakingEngine.Tests.Application.Commands;

public class AcceptMatchCommandHandlerTests
{
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AcceptMatchCommandHandler _handler;
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock;

    public AcceptMatchCommandHandlerTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<AcceptMatchCommandHandler>>();
        _backgroundJobServiceMock = new Mock<IBackgroundJobService>();

        _handler = new AcceptMatchCommandHandler(
           _matchRepositoryMock.Object,
           _unitOfWorkMock.Object,
           loggerMock.Object,
           _backgroundJobServiceMock.Object);
    }

    [Fact]
    public async Task Handle_AllPlayersAccept_MatchStarts()
    {
        var matchId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var match = new Match(matchId, 1500, DateTimeOffset.UtcNow, GameMode.Solo)
        {
            Status = MatchStatus.Pending,
            Players = new List<MatchPlayer>
            {
                new MatchPlayer { PlayerId = player1Id, Accepted = false },
                new MatchPlayer { PlayerId = player2Id, Accepted = true }
            }
        };

        _matchRepositoryMock.Setup(repo => repo.GetByIdWithPlayersAsync(matchId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _backgroundJobServiceMock.Setup(x => x.Schedule(
            It.IsAny<System.Linq.Expressions.Expression<Action<IMediator>>>(),
            It.IsAny<TimeSpan>()))
            .Returns("test-job-id");

        var command = new AcceptMatchCommand(player1Id, matchId);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.AllAccepted);
        Assert.Equal(MatchStatus.MapVeto, match.Status);
        Assert.True(match.Players.First(p => p.PlayerId == player1Id).Accepted);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}