using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace MatchmakingEngine.Hubs;

[Authorize]
public class MatchmakingHub : Hub
{
    private readonly ILogger<MatchmakingHub> _logger;

    public MatchmakingHub(ILogger<MatchmakingHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var playerIdString = Context.User?.FindFirst("PlayerId")?.Value;
        var roleString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(playerIdString))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, playerIdString!);

            if (roleString == "Admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            _logger.LogInformation("Verified Player {PlayerId} connected. ConnectionId: {ConnectionId}",
               playerIdString, Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning("Connection rejected. No PlayerId found in token");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Player disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
