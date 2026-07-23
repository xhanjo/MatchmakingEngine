namespace MatchmakingEngine.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expirationTime = null) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
