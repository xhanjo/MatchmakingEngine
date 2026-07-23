using System.Text.Json;
using MatchmakingEngine.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace MatchmakingEngine.Infrastructure.Services;

public class TwoLevelCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    public TwoLevelCacheService(IMemoryCache memoryCache, IDistributedCache distributedCache)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expirationTime = null) where T : class
    {
        if (_memoryCache.TryGetValue(key, out T? memoryValue))
            return memoryValue;

        var distributedValue = await _distributedCache.GetStringAsync(key);
        if (!string.IsNullOrEmpty(distributedValue))
        {
            var value = JsonSerializer.Deserialize<T>(distributedValue);

            SetMemoryCache(key, value, expirationTime);
            return value;
        }

        var freshValue = await factory();

        if (freshValue != null)
        {
            var options = new DistributedCacheEntryOptions();
            if (expirationTime.HasValue)
                options.AbsoluteExpirationRelativeToNow = expirationTime.Value;

            await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(freshValue), options);

            SetMemoryCache(key, freshValue, expirationTime);
        }
        return freshValue;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    private void SetMemoryCache<T>(string key, T value, TimeSpan? expirationTime)
    {
        var cacheOptions = new MemoryCacheEntryOptions();
        if (expirationTime.HasValue)
            cacheOptions.SetAbsoluteExpiration(expirationTime.Value);

        _memoryCache.Set(key, value, cacheOptions);
    }
}
