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
    private readonly ICacheService _cacheService;
    private readonly ILeaderboardService _leaderboardService;
    public RegisterPlayerCommandHandler(
        IPlayerRepository playerRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILeaderboardService leaderboardService)
    {
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _leaderboardService = leaderboardService;
    }

    public async Task<PlayerResponseDto> Handle(RegisterPlayerCommand request, CancellationToken cancellationToken)
    {
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

        await _cacheService.RemoveAsync("all_players", cancellationToken);

        await _leaderboardService.UpdatePlayerMmrAsync(player.Id, player.Mmr);

        return new PlayerResponseDto(
            player.Id, player.Username, player.Mmr, player.TrustFactor, player.Region, player.Role, player.CreatedAt
        );
    }
}
