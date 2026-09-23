using MatchmakingEngine.Application.Interfaces;
using StackExchange.Redis;

namespace MatchmakingEngine.Infrastructure.Services;

public class RedisDistributedLockService(IConnectionMultiplexer redisConnection) : IDistributedLockService
{
    private readonly IDatabase _redisDb = redisConnection.GetDatabase();

    public async Task<bool> TryAcquireLockAsync(string key, string lockValue, TimeSpan expiry)
    {
        return await _redisDb.LockTakeAsync(key, lockValue, expiry);
    }

    public async Task<bool> ReleaseLockAsync(string key, string lockValue)
    {
        return await _redisDb.LockReleaseAsync(key, lockValue);
    }
}
