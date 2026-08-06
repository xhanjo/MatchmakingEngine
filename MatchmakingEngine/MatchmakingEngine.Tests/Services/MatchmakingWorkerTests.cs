using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Hubs;
using MatchmakingEngine.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameMatch = MatchmakingEngine.Domain.Match;

namespace MatchmakingEngine.Tests.Services;

public class MatchmakingWorkerTests
{
    private readonly Mock<IMatchmakingQueue> _mockQueue = new();
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory = new();
    private readonly Mock<ILogger<MatchmakingWorker>> _mockLogger = new();
    private readonly Mock<IHubContext<MatchmakingHub>> _mockHubContext = new();

    private MatchmakingWorker CreateWorker() =>
        new MatchmakingWorker(_mockQueue.Object, _mockLogger.Object, _mockScopeFactory.Object, _mockHubContext.Object);

    [Fact]
    public async Task ProcessEvaluationAsync_Should_Not_Match_If_Mmr_Diff_Exceeds_Delta()
    {
        var worker = CreateWorker();
        var anchorPlayerId = Guid.NewGuid();

        var anchorTicket = new MatchmakingTicket(anchorPlayerId, anchorPlayerId, "Player1", 1000, 1.0, PlayerRegion.EuWest, DateTimeOffset.UtcNow, GameMode.Solo, null);

        _mockQueue.Setup(q => q.GetTicketAsync(anchorPlayerId)).ReturnsAsync(anchorTicket);

        _mockQueue.Setup(q => q.GetCandidatesByMmrRangeAsync(It.IsAny<PlayerRegion>(), It.IsAny<GameMode>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new[] { anchorPlayerId });

        await worker.ProcessEvaluationAsync(anchorPlayerId, GameMode.Solo, CancellationToken.None);

        _mockQueue.Verify(q => q.EnqueueAsync(anchorTicket), Times.Once);
    }
}