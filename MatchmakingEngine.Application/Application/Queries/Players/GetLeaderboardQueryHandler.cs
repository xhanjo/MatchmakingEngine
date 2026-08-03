using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.Players;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntryDto>>
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IPlayerRepository _playerRepository;

    public GetLeaderboardQueryHandler(ILeaderboardService leaderboardService, IPlayerRepository playerRepository)
    {
        _leaderboardService = leaderboardService;
        _playerRepository = playerRepository;        
    }

    public async Task<List<LeaderboardEntryDto>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var topPlayers = await _leaderboardService.GetTopPlayersAsync(request.Count);

        if (!topPlayers.Any())
            return new List<LeaderboardEntryDto>();

        var playerIds = topPlayers.Select(p => p.playerId).ToList();

        var playersFromDb = await _playerRepository.GetByIdsAsync(playerIds, trackChanges: false, cancellationToken);

        var playerDict = playersFromDb.ToDictionary(p => p.Id);

        var result = new List<LeaderboardEntryDto>();
        long currentRank = 1;

        foreach (var p in topPlayers)
        {
            if (playerDict.TryGetValue(p.playerId, out var playerInfo))
            {
                result.Add(new LeaderboardEntryDto(
                    currentRank++,
                    playerInfo.Id,
                    playerInfo.Username,
                    playerInfo.Mmr,
                    playerInfo.Region
                    ));
            }
        }
        return result;
    }
}
