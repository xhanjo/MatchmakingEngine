using BCrypt.Net;
using MatchmakingEngine.Application.Commands.Auth;
using MatchmakingEngine.Configuration;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchmakingEngine.Tests.Application.Commands.Auth;

public class LoginCommandHandlerTests : IDisposable
{
    private readonly MatchmakingDbContext _context;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _context = TestDbContextFactory.Create();

        _jwtSettings = Options.Create(new JwtSettings
        {
            Key = "supers-secret-test-key-32-chars-minimum",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });

        _handler = new LoginCommandHandler(_context, _jwtSettings);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var password = "SecurePassword123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var player = new Player { Id = Guid.NewGuid(), Username = "TestUser", PasswordHash = hash };
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        var command = new LoginCommand("TestUser", password);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        var player = new Player { Id = Guid.NewGuid(), Username = "TestUser", PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword") };
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        var command = new LoginCommand("TestUser", "WrongPassword");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownUsername_ThrowsUnauthorizedAccessException()
    {
        var command = new LoginCommand("UnknownUser", "AnyPassword");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
