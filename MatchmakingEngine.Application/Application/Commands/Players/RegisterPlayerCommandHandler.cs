using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain;
using BCrypt.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Application.Interfaces;

namespace MatchmakingEngine.Application.Commands.Players;

public class RegisterPlayerCommandHandler : IRequestHandler<RegisterPlayerCommand, PlayerResponseDto>
{
    private readonly IMatchmakingDbContext _context;
    private readonly IDistributedCache _cache;

    public RegisterPlayerCommandHandler(IMatchmakingDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PlayerResponseDto> Handle(RegisterPlayerCommand request, CancellationToken cancellationToken)
    {
        if (await _context.Players.AnyAsync(p => p.Username == request.Username, cancellationToken))
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");

        var player = new Player
        {
            Username = request.Username,
            Region = request.Region,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = PlayerRole.Player
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync("all_players", cancellationToken);

        return new PlayerResponseDto(
            player.Id, player.Username, player.Mmr, player.TrustFactor, player.Region, player.Role, player.CreatedAt
            );
    }
}
