using MediatR;
using MatchmakingEngine.DTO;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;

namespace MatchmakingEngine.Application.Queries.Players;

public class GetAllPlayersQueryHandler : IRequestHandler<GetAllPlayersQuery, List<PlayerResponseDto>>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IDistributedCache _cache;

    public GetAllPlayersQueryHandler(IPlayerRepository playerRepository, IDistributedCache cache)
    {
        _playerRepository = playerRepository;
        _cache = cache;
    }

    public async Task<List<PlayerResponseDto>> Handle(GetAllPlayersQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = "all_players";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
            return JsonSerializer.Deserialize<List<PlayerResponseDto>>(cachedData) ?? new List<PlayerResponseDto>();

        var players = await _playerRepository.GetAllAsync(trackChanges: false);

        var dtos = players.Select(p => new PlayerResponseDto(
            p.Id, p.Username, p.Mmr, p.TrustFactor, p.Region, p.Role, p.CreatedAt
            )).ToList();

        var cachedOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), cachedOptions, cancellationToken);

        return dtos;
    }
}
