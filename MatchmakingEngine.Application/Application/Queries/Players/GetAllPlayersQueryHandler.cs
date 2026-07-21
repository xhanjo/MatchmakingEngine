using MediatR;
using MatchmakingEngine.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using MatchmakingEngine.Application.Interfaces;

namespace MatchmakingEngine.Application.Queries.Players;

public class GetAllPlayersQueryHandler : IRequestHandler<GetAllPlayersQuery, List<PlayerResponseDto>>
{
    private readonly IMatchmakingDbContext _context;
    private readonly IDistributedCache _cache;

    public GetAllPlayersQueryHandler(IMatchmakingDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<PlayerResponseDto>> Handle(GetAllPlayersQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = "all_players";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
            return JsonSerializer.Deserialize<List<PlayerResponseDto>>(cachedData) ?? new List<PlayerResponseDto>();

        var players = await _context.Players
            .AsNoTracking()
            .ToListAsync(cancellationToken);

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
