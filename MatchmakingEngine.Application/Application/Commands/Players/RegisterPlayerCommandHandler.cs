using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain;
using BCrypt.Net;
using Microsoft.Extensions.Caching.Distributed;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;

namespace MatchmakingEngine.Application.Commands.Players;

public class RegisterPlayerCommandHandler : IRequestHandler<RegisterPlayerCommand, PlayerResponseDto>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;

    public RegisterPlayerCommandHandler(
        IPlayerRepository playerRepository,
        IUnitOfWork unitOfWork,
        IDistributedCache cache)
    {
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<PlayerResponseDto> Handle(RegisterPlayerCommand request, CancellationToken cancellationToken)
    {
        //if (await _context.Players.AnyAsync(p => p.Username == request.Username, cancellationToken))
        var existingPlayer = await _playerRepository.GetByUsernameAsync(request.Username);
        if (existingPlayer != null)
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");

        var player = new Player
        {
            Username = request.Username,
            Region = request.Region,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = PlayerRole.Player
        };

        await _playerRepository.AddAsync(player);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync("all_players", cancellationToken);

        return new PlayerResponseDto(
            player.Id, player.Username, player.Mmr, player.TrustFactor, player.Region, player.Role, player.CreatedAt
        );
    }
}
