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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();

                var timeoutThreshold = DateTime.UtcNow.AddSeconds(-30);

                var expiredMatches = await dbContext.Matches
                    .Where(m => m.Status == MatchStatus.Pending && m.CreatedAt < timeoutThreshold)
                    .ToListAsync(stoppingToken);

                if (expiredMatches.Any())
                {
                    foreach (var match in expiredMatches)
                    {
                        _logger.LogInformation("Match {MatchId} timed out. Canceling.", match.Id);
                        match.Status = MatchStatus.Canceled;

                        await _hubContext.Clients.User(match.Player1Id.ToString()).SendAsync("MatchCanceled", cancellationToken: stoppingToken);
                        await _hubContext.Clients.User(match.Player2Id.ToString()).SendAsync("MatchCanceled", cancellationToken: stoppingToken);
                    }
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing MatchCleanupWorker.");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}