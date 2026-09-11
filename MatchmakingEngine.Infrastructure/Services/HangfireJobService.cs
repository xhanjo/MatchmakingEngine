using MatchmakingEngine.Application.Interfaces;
using System.Linq.Expressions;
using Hangfire;

namespace MatchmakingEngine.Infrastructure.Services;

public class HangfireJobService(IBackgroundJobClient backgroundJobClient) : IBackgroundJobService
{
    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        return backgroundJobClient.Schedule<T>(methodCall, delay);
    }

    public void Delete(string jobId)
    {
        backgroundJobClient.Delete(jobId);
    }
}
