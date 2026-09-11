using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.Players;

public class GetAllPlayersQueryHandler : IRequestHandler<GetAllPlayersQuery, List<PlayerResponseDto>>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly ICacheService _cacheService;

    public GetAllPlayersQueryHandler(IPlayerRepository playerRepository, ICacheService cacheService)
    {
        _playerRepository = playerRepository;
        _cacheService = cacheService;
    }

    public async Task<List<PlayerResponseDto>> Handle(GetAllPlayersQuery request, CancellationToken cancellationToken)
    {
        var cachedPlayers = await _cacheService.GetOrCreateAsync(
            key: "all_players",
            factory: async () =>
            {
                var players = await _playerRepository.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
                return players.Select(p => new PlayerResponseDto(
                    p.Id, p.Username, p.Mmr, p.TrustFactor, p.Region, p.Role, p.CreatedAt
                    )).ToList();
            }, 
            expirationTime: TimeSpan.FromMinutes(5)
            );

        return cachedPlayers ?? new List<PlayerResponseDto>();
    }
}
