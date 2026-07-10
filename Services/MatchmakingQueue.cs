using MatchmakingEngine.Domain;
using Microsoft.AspNetCore.Mvc.Routing;
using StackExchange.Redis;
using System.Collections;
using System.Text.Json;

namespace MatchmakingEngine.Services;

public class MatchmakingQueue : IMatchmakingQueue
{
    private readonly IDatabase _redisDb;
    private const string QueueKey = "matchmaking_queue";
    private const string ActivePlayersHashKey = "active_players";

    public MatchmakingQueue(IConnectionMultiplexer redisConnection)
    {
        _redisDb = redisConnection.GetDatabase();
    }
    public async ValueTask EnqueueAsync(MatchmakingTicket ticket)
    {
        var json = JsonSerializer.Serialize(ticket);
        await _redisDb.ListRightPushAsync(QueueKey, json);
        await _redisDb.HashSetAsync(ActivePlayersHashKey, ticket.PlayerId.ToString(), json);
    }

    public async ValueTask<MatchmakingTicket?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var redisValue = await _redisDb.ListLeftPopAsync(QueueKey);

        if (redisValue.HasValue == false)
        {
            await Task.Delay(1000, cancellationToken);
            return null;
        }
        return JsonSerializer.Deserialize<MatchmakingTicket>(redisValue.ToString());
    }

    public bool IsPlayerInQueue(Guid playerId)
    {
        return _redisDb.HashExists(ActivePlayersHashKey, playerId.ToString());
    }

    public void RemovePlayer(Guid playerId)
    {
        _redisDb.HashDelete(ActivePlayersHashKey, playerId.ToString());
    }
}
