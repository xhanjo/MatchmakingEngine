using BCrypt.Net;
using MatchmakingEngine.Application.Commands.Players;
using MatchmakingEngine.Data;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchmakingEngine.Tests.Application.Commands.Auth;

public class RegisterPlayerCommandHandlerTests : IDisposable
{
    private readonly MatchmakingDbContext _context;
    private readonly RegisterPlayerCommandHandler _handler;
    private readonly Mock<IDistributedCache> _cacheMock;

    public RegisterPlayerCommandHandlerTests()
    {
        _context = TestDbContextFactory.Create();
        _cacheMock = new Mock<IDistributedCache>();
        _handler = new RegisterPlayerCommandHandler(_context, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_ValidData_CreatesUserWithHashedPassword()
    {
        var command = new RegisterPlayerCommand("NewUser", "Password123", Domain.PlayerRegion.EuWest);

        var responseDto = await _handler.Handle(command, CancellationToken.None);
        var playerInDb = await _context.Players.FindAsync(responseDto.Id);

        Assert.NotNull(playerInDb);
        Assert.Equal("NewUser", playerInDb.Username);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123", playerInDb.PasswordHash));
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsException()
    {
        var command = new RegisterPlayerCommand("ExistingUser", "Password123", Domain.PlayerRegion.EuEast);
        await _handler.Handle(command, CancellationToken.None);

        var duplicateCommand = new RegisterPlayerCommand("ExistingUser", "DifferentPassword", Domain.PlayerRegion.Asia);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(duplicateCommand, CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
