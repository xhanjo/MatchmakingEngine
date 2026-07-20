using MatchmakingEngine.Domain;
using StackExchange.Redis;
using System.Text.Json;

namespace MatchmakingEngine.Services;

public class MatchmakingQueue : IMatchmakingQueue
{
    private readonly IDatabase _redisDb;
    private const string EvaluationQueueKey = "matchmaking_queue";
    private const string ActivePlayersHashKey = "active_players";

    public MatchmakingQueue(IConnectionMultiplexer redisConnection)
    {
        _redisDb = redisConnection.GetDatabase();
    }

    public async ValueTask EnqueueAsync(MatchmakingTicket ticket)
    {
        var playerIdStr = ticket.PlayerId.ToString();
        var json = JsonSerializer.Serialize(ticket);

        await _redisDb.HashSetAsync(ActivePlayersHashKey, playerIdStr, json);

        await _redisDb.ListRightPushAsync(EvaluationQueueKey, playerIdStr);

        await _redisDb.SortedSetAddAsync($"mmr_index:{ticket.Region}", playerIdStr, ticket.Mmr);
    }

    public async ValueTask<Guid?> DequeueEvaluationIdAsync(CancellationToken cancellationToken = default)
    {
        var redisValue = await _redisDb.ListLeftPopAsync(EvaluationQueueKey);

        if (!redisValue.HasValue)
        {
            await Task.Delay(1000, cancellationToken);
            return null;
        }

        return Guid.Parse(redisValue.ToString());
    }

    public async ValueTask<MatchmakingTicket?> GetTicketAsync(Guid playerId)
    {
        var json = await _redisDb.HashGetAsync(ActivePlayersHashKey, playerId.ToString());

        if (!json.HasValue)
            return null;

        return JsonSerializer.Deserialize<MatchmakingTicket>(json.ToString());
    }

    public async ValueTask<Guid[]> GetCandidatesByMmrRangeAsync(PlayerRegion region,double minMmr, double maxMmr )
    {
        var indexKey = $"mmr_index:{region}";

        var values = await _redisDb.SortedSetRangeByScoreAsync(indexKey, start: minMmr, stop: maxMmr);

        return values.Select(v => Guid.Parse(v.ToString())).ToArray();
    }

    public async ValueTask<bool> IsPlayerInQueueAsync(Guid playerId)
    {
        return await _redisDb.HashExistsAsync(ActivePlayersHashKey, playerId.ToString());
    }

    public async ValueTask RemovePlayerAsync(MatchmakingTicket ticket)
    {
        var playerIdStr = ticket.PlayerId.ToString();
        var indexKey = $"mmr_index:{ticket.Region}";

        var hashTask = _redisDb.HashDeleteAsync(ActivePlayersHashKey, playerIdStr);
        var setTask = _redisDb.SortedSetRemoveAsync(indexKey, playerIdStr);

        await Task.WhenAll(hashTask, setTask);
    }
}
