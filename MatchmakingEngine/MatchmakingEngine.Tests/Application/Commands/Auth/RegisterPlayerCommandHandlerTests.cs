using BCrypt.Net;
using MatchmakingEngine.Application.Commands.Players;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchmakingEngine.Tests.Application.Commands.Auth;

public class RegisterPlayerCommandHandlerTests
{
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly RegisterPlayerCommandHandler _handler;
    

    public RegisterPlayerCommandHandlerTests()
    {
        _playerRepoMock = new Mock<IPlayerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IDistributedCache>();

        _handler = new RegisterPlayerCommandHandler(
            _playerRepoMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_ValidData_CreatesUserWithHashedPassword()
    {
        var command = new RegisterPlayerCommand("NewUser", "Password123", Domain.PlayerRegion.EuWest);

        _playerRepoMock
            .Setup(repo => repo.GetByUsernameAsync(command.Username, false))
            .ReturnsAsync((Player?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(command.Username, result.Username);

        _playerRepoMock.Verify(repo => repo.AddAsync(It.Is<Player>(p =>
            p.Username == command.Username &&
            p.Region == command.Region)), Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsException()
    {
        var command = new RegisterPlayerCommand("ExistingUser", "Password123", Domain.PlayerRegion.EuEast);
      
        _playerRepoMock
            .Setup(repo => repo.GetByUsernameAsync(command.Username, false))
            .ReturnsAsync(new Player { Username = "ExistingUser" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _playerRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Player>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
