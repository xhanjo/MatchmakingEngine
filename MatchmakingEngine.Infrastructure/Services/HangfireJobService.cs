using MatchmakingEngine.Application.Interfaces;
using System;
using System.Linq.Expressions;
using Hangfire;

namespace MatchmakingEngine.Infrastructure.Services;

public class HangfireJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireJobService(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule<T>(methodCall, delay);
    }

    public void Delete(string jobId)
    {
        _backgroundJobClient.Delete(jobId);
    }
}
