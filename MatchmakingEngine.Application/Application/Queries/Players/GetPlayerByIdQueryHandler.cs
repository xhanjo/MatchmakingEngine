using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;

namespace MatchmakingEngine.Application.Queries.Players;

public class GetPlayerByIdQueryHandler : IRequestHandler<GetPlayerByIdQuery, PlayerResponseDto>
{
    private readonly IPlayerRepository _playerRepository;

    public GetPlayerByIdQueryHandler(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<PlayerResponseDto> Handle(GetPlayerByIdQuery request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByIdAsync(request.Id, trackChanges: false);

        if (player == null)
            throw new NotFoundException($"Player with ID {request.Id} was not found");

        return new PlayerResponseDto(
            player.Id, player.Username, player.Mmr, player.TrustFactor, player.Region, player.Role, player.CreatedAt
        );
    }
}
