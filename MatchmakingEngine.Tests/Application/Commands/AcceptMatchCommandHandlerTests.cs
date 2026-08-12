using Hangfire;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands;

public class AcceptMatchCommandHandlerTests
{
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AcceptMatchCommandHandler>> _loggerMock;
    private readonly AcceptMatchCommandHandler _handler;
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock;

    public AcceptMatchCommandHandlerTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AcceptMatchCommandHandler>>();
        _backgroundJobServiceMock = new Mock<IBackgroundJobService>();

        _handler = new AcceptMatchCommandHandler(
           _matchRepositoryMock.Object,
           _unitOfWorkMock.Object,
           _loggerMock.Object,
           _backgroundJobServiceMock.Object);
    }

    [Fact]
    public async Task Handle_AllPlayersAccept_MatchStarts()
    {
        var matchId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var match = new Domain.Match(matchId, 1500, DateTimeOffset.UtcNow, GameMode.Solo)
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

        _backgroundJobServiceMock.Setup(x => x.Schedule<IMediator>(
            It.IsAny<System.Linq.Expressions.Expression<System.Action<IMediator>>>(),
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