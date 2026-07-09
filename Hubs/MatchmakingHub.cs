using Microsoft.AspNetCore.SignalR;

namespace MatchmakingEngine.Hubs;

public class MatchmakingHub : Hub
{
    private readonly ILogger<MatchmakingHub> _logger;

    public MatchmakingHub(ILogger<MatchmakingHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var playerIdString = httpContext?.Request.Query["playerId"];

        if (!string.IsNullOrEmpty(playerIdString))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, playerIdString!);
            _logger.LogInformation("Player is connected {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Player disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
