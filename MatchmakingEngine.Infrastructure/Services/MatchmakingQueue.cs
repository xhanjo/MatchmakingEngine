using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace MatchmakingEngine.Infrastructure.Services;

public class MatchmakingQueue : IMatchmakingQueue
{
    private readonly IDatabase _redisDb;
    private const string ActivePlayersHashKey = "active_players";

    public MatchmakingQueue(IConnectionMultiplexer redisConnection)
    {
        _redisDb = redisConnection.GetDatabase();
    }

    private string GetEvaluationQueueKey(GameMode mode) => $"matchmaking_queue_{mode}";
    private string GetProcessingQueueKey(GameMode mode) => $"matchmaking_queue_processing_{mode}";
    private string GetMmrIndexKey(GameMode mode, PlayerRegion region) => $"Mmr_index:{mode}:{region}";

    public async ValueTask EnqueueAsync(MatchmakingTicket ticket)
    {
        var playerIdStr = ticket.PlayerId.ToString();
        var json = JsonSerializer.Serialize(ticket);

        await _redisDb.HashSetAsync(ActivePlayersHashKey, playerIdStr, json);

        await _redisDb.ListRemoveAsync(GetProcessingQueueKey(ticket.GameMode), playerIdStr);

        await _redisDb.ListLeftPushAsync(GetEvaluationQueueKey(ticket.GameMode), playerIdStr);

        await _redisDb.SortedSetAddAsync(GetMmrIndexKey(ticket.GameMode, ticket.Region), playerIdStr, ticket.Mmr);
    }

    public async ValueTask<Guid?> DequeueEvaluationIdAsync(GameMode gameMode, CancellationToken cancellationToken = default)
    {
        var redisValue = await _redisDb.ListRightPopLeftPushAsync(
            GetEvaluationQueueKey(gameMode),
            GetProcessingQueueKey(gameMode));

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

    public async ValueTask<Guid[]> GetCandidatesByMmrRangeAsync(PlayerRegion region, GameMode gameMode, double minMmr, double maxMmr )
    {
        var indexKey = GetMmrIndexKey(gameMode, region);

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

        var hashTask = _redisDb.HashDeleteAsync(ActivePlayersHashKey, playerIdStr);
        var setTask = _redisDb.SortedSetRemoveAsync(GetMmrIndexKey(ticket.GameMode, ticket.Region), playerIdStr);
        var listTask = _redisDb.ListRemoveAsync(GetProcessingQueueKey(ticket.GameMode), playerIdStr);

        await Task.WhenAll(hashTask, setTask, listTask);
    }
}
