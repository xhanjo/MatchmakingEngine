using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchmakingEngine.HostedServices;

public class MatchCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<MatchmakingHub> _hubContext;
    private readonly ILogger<MatchCleanupWorker> _logger;

    public MatchCleanupWorker(IServiceProvider serviceProvider, IHubContext<MatchmakingHub> hubContext, ILogger<MatchCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();

                var timeoutThreshold = DateTime.UtcNow.AddSeconds(-30);

                var expiredMatches = await dbContext.Matches
                    .Include(m => m.Players)
                    .Where(m => m.Status == MatchStatus.Pending && m.CreatedAt < timeoutThreshold)
                    .ToListAsync(cancellationToken);

                if (expiredMatches.Any())
                {
                    foreach (var match in expiredMatches)
                    {
                        _logger.LogInformation("Match {MatchId} timed out. Canceling.", match.Id);
                        match.Status = MatchStatus.Canceled;

                        foreach(var mp in match.Players)
                        {
                            await _hubContext.Clients.User(mp.PlayerId.ToString()).SendAsync("MatchCanceled", cancellationToken);

                        }
                    }
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing MatchCleanupWorker.");
            }

            await Task.Delay(5000, cancellationToken);
        }
    }
}