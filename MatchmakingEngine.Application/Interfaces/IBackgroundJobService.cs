using System;
using System.Linq.Expressions;

namespace MatchmakingEngine.Application.Interfaces;

public interface IBackgroundJobService 
{
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);
    void Delete(string jobId);
}
