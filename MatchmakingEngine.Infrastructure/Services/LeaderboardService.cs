using MatchmakingEngine.Application.Interfaces;
using StackExchange.Redis;

namespace MatchmakingEngine.Infrastructure.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly IDatabase _redisDb;
    private const string LeaderboardKey = "global_leaderboard";

    public LeaderboardService(IConnectionMultiplexer redisConnection)
    {
        _redisDb = redisConnection.GetDatabase();
    }

    public async Task UpdatePlayerMmrAsync(Guid playerId, int mmr)
    {
        await _redisDb.SortedSetAddAsync(LeaderboardKey, playerId.ToString(), mmr);
    }
    public async Task<IEnumerable<(Guid playerId, int mmr)>> GetTopPlayersAsync(int count = 100)
    {
        var topPlayers = await _redisDb.SortedSetRangeByRankWithScoresAsync(
            LeaderboardKey,
            start: 0,
            stop: count - 1,
            order: Order.Descending);

        var result = new List<(Guid playerId, int mmr)>();
        foreach (var entry in topPlayers)
        {
            if (Guid.TryParse(entry.Element.ToString(), out var playerId))
            {
                result.Add((playerId, (int)entry.Score));
            }
        }
        return result;
    }

    public async Task<long?> GetPlayerRankAsync(Guid playerId)
    {
        var rank = await _redisDb.SortedSetRankAsync(LeaderboardKey, playerId.ToString(), Order.Descending);

        if (rank.HasValue)
        {
            return rank.Value + 1;
        }
        return null;
    }
}
