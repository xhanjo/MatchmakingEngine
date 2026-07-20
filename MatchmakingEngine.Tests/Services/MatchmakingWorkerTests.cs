using FluentAssertions;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Hubs;
using MatchmakingEngine.Services;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Services;

public class MatchmakingWorkerTests
{
    private readonly Mock<IMatchmakingQueue> _queueMock = new();
    private readonly Mock<IHubContext<MatchmakingHub>> _hubMock = new();
    private readonly Mock<ILogger<MatchmakingWorker>> _loggerMock = new();

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();

    public MatchmakingWorkerTests()
    {
        var dbContext = TestDbContextFactory.Create();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(Data.MatchmakingDbContext))).Returns(dbContext);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(_scopeMock.Object);
    }

    [Fact]
    public async Task ProcessEvaluationAsync_NoOpponentFound_ShouldReEnqueueAnchor()
    {
        var anchorId = Guid.NewGuid();

        var anchorTicket = new MatchmakingTicket(
            Guid.NewGuid(),
            anchorId,
            "TestPlayer",
            1000.0,
            0.5,
            PlayerRegion.EuWest,
            DateTimeOffset.UtcNow
            );

        _queueMock.Setup(q => q.GetTicketAsync(anchorId)).ReturnsAsync(anchorTicket);

        _queueMock.Setup(q => q.GetCandidatesByMmrRangeAsync(
            It.IsAny<PlayerRegion>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var worker = new MatchmakingWorker(
            _queueMock.Object,
            _loggerMock.Object,
            _scopeFactoryMock.Object,
            _hubMock.Object
            );

        await worker.ProcessEvaluationAsync(anchorId, CancellationToken.None);

        _queueMock.Verify(q => q.EnqueueAsync(It.Is<MatchmakingTicket>(t => t.PlayerId == anchorId)), Times.Once);

        _queueMock.Verify(q => q.RemovePlayerAsync(It.IsAny<MatchmakingTicket>()), Times.Never);
    }

    [Fact]
    public async Task ProcessEvaluationAsync_OpponentFound_ShouldCreateMatchAndRemoveFromQueue()
    {
        var anchorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();

        var anchorTicket = new MatchmakingTicket(Guid.NewGuid(), anchorId, "Player1", 1000.0, 0.5, PlayerRegion.EuWest, DateTimeOffset.UtcNow);
        var opponentTicket = new MatchmakingTicket(Guid.NewGuid(), opponentId, "Player2", 1000.0, 0.5, PlayerRegion.EuWest, DateTimeOffset.UtcNow);

        _queueMock.Setup(q => q.GetTicketAsync(anchorId)).ReturnsAsync(anchorTicket);
        _queueMock.Setup(q => q.GetTicketAsync(opponentId)).ReturnsAsync(opponentTicket);

        _queueMock.Setup(q => q.GetCandidatesByMmrRangeAsync(
            It.IsAny<PlayerRegion>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new[] { opponentId });

        var singleClientProxyMock = new Mock<ISingleClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(singleClientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(singleClientProxyMock.Object);
        _hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var worker = new MatchmakingWorker(
            _queueMock.Object,
            _loggerMock.Object,
            _scopeFactoryMock.Object,
            _hubMock.Object);

        await worker.ProcessEvaluationAsync(anchorId, CancellationToken.None);

        _queueMock.Verify(q => q.RemovePlayerAsync(It.Is<MatchmakingTicket>(t => t.PlayerId == anchorId)), Times.Once);
        _queueMock.Verify(q => q.RemovePlayerAsync(It.Is<MatchmakingTicket>(t => t.PlayerId == opponentId)), Times.Once);

        _queueMock.Verify(q => q.EnqueueAsync(It.IsAny<MatchmakingTicket>()), Times.Never);

        singleClientProxyMock.Verify(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Exactly(2));
    }
}
