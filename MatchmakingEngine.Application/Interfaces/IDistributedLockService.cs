namespace MatchmakingEngine.Application.Interfaces;

public interface IDistributedLockService
{
    Task<bool> TryAcquireLockAsync(string key, string lockValue, TimeSpan expiry);
    Task<bool> ReleaseLockAsync(string key, string lockValue);
}
