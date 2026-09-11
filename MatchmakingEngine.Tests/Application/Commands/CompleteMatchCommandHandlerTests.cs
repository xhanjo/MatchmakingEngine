using MatchmakingEngine.Application.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using Microsoft.Extensions.Logging;
using Moq;
using Match = MatchmakingEngine.Domain.Match;

namespace MatchmakingEngine.Tests.Application.Commands;

public class CompleteMatchCommandHandlerTests
{
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CompleteMatchCommandHandler _handler;

    public CompleteMatchCommandHandlerTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<CompleteMatchCommandHandler>>();
        var cacheServiceMock = new Mock<ICacheService>();
        var leaderboardService = new Mock<ILeaderboardService>();

        _handler = new CompleteMatchCommandHandler(
            _matchRepositoryMock.Object,
            _unitOfWorkMock.Object,
            loggerMock.Object,
            cacheServiceMock.Object,
            leaderboardService.Object);
    }

    [Fact]
    public async Task Handle_ValidMatch_CompletesMatchAndGeneratesStats()
    {
        var matchId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var player1 = new Player { Id = player1Id, Username = "Player1", Mmr = 1500 };
        var player2 = new Player { Id = player2Id, Username = "Player2", Mmr = 1500 };

        var match = new Match(matchId, 1500, DateTimeOffset.UtcNow, GameMode.Solo)
        {
            Status = MatchStatus.Accepted,
            Players = new List<MatchPlayer>
            {
                new MatchPlayer { PlayerId = player1Id, Team = 1, Player = player1 },
                new MatchPlayer { PlayerId = player2Id, Team = 2, Player = player2 }
            }
        };

        _matchRepositoryMock.Setup(repo => repo.GetByIdWithPlayersAsync(matchId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var command = new CompleteMatchCommand(matchId);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(MatchStatus.Finished, match.Status); 
        Assert.NotNull(result.Scoreboard);
        Assert.Equal(2, result.Scoreboard.Count);

        Assert.True(player1.Mmr != 1500);
        Assert.True(player2.Mmr != 1500);

        Assert.Contains(result.Scoreboard, s => s.IsMvp);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}