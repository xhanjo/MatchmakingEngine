using MatchmakingEngine.Application.Application.Commands.Auth;
using MatchmakingEngine.Application.Configuration;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Auth;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MatchmakingEngine.Tests.Application.Commands.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly LoginCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock; 

    public LoginCommandHandlerTests()
    {
        _playerRepoMock = new Mock<IPlayerRepository>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new LoginCommandHandler(_playerRepoMock.Object, _jwtProviderMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var password = "SecurePassword123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var player = new Player { Id = Guid.NewGuid(), Username = "TestUser", PasswordHash = hash };

        _playerRepoMock.Setup(x => x.GetByUsernameAsync("TestUser", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);
        _jwtProviderMock.Setup(x => x.GenerateToken(It.IsAny<Player>())).Returns("fake-access-token");

        _jwtProviderMock.Setup(x => x.GenerateRefreshToken()).Returns("fake-refresh-token");
        
        var command = new LoginCommand("TestUser", password);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("fake-access-token", result.AccessToken);
        Assert.Equal("fake-refresh-token", result.RefreshToken);
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        var player = new Player { Id = Guid.NewGuid(), Username = "TestUser", PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword") };
        _playerRepoMock.Setup(x => x.GetByUsernameAsync("TestUser", false)).ReturnsAsync(player);

        var command = new LoginCommand("TestUser", "WrongPassword");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownUsername_ThrowsUnauthorizedAccessException()
    {
        _playerRepoMock.Setup(x => x.GetByUsernameAsync("UnknownUser", false)).ReturnsAsync((Player?)null);

        var command = new LoginCommand("UnknownUser", "AnyPassword");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }
}