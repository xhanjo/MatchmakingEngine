using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;

namespace MatchmakingEngine.HostedServices;

public class LeaderboardSeederWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public LeaderboardSeederWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var leaderboardService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();
        var playerRepository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        try
        {
            var topPlayers = await leaderboardService.GetTopPlayersAsync(1);
            if (!topPlayers.Any())
            {
                var allPlayers = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);

                foreach (var p in allPlayers)
                {
                    await leaderboardService.UpdatePlayerMmrAsync(p.Id, p.Mmr);
                }
            }
        }
        catch (Exception)
        {

        }
    }
}
