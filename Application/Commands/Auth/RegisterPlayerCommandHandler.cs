using BCrypt.Net;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchmakingEngine.Application.Commands.Auth;

public class RegisterPlayerCommandHandler : IRequestHandler<RegisterPlayerCommand, Guid>
{
    private readonly MatchmakingDbContext _context;

    public RegisterPlayerCommandHandler(MatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(RegisterPlayerCommand request, CancellationToken cancellationToken)
    {
        var usernameExists = await _context.Players.AnyAsync(p => p.Username == request.Username, cancellationToken);

        if (usernameExists)
            throw new InvalidOperationException("Username already exists");


        var player = new Player
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Region = request.Region
        };


        _context.Players.Add(player);
        await _context.SaveChangesAsync(cancellationToken);

        return player.Id;
    }
}
