using MatchmakingEngine.Application.Application.Commands.Friends;
using MatchmakingEngine.Application.Application.Queries.Friends;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MatchmakingEngine.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<MatchmakingEngine.Hubs.MatchmakingHub> _hubContext;

    public FriendsController(IMediator mediator, Microsoft.AspNetCore.SignalR.IHubContext<MatchmakingEngine.Hubs.MatchmakingHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    private Guid GetPlayerId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }

    [HttpPost("request/{targetPlayerId}")]
    public async Task<IActionResult> SendRequest(Guid targetPlayerId)
    {
        var result = await _mediator.Send(new SendFriendRequestCommand(GetPlayerId(), targetPlayerId));
        if (result)
            await _hubContext.Clients.Group(targetPlayerId.ToString()).SendAsync("FriendsUpdated");
        return Ok(new { success = result });
    }

    [HttpPost("accept/{requestId}")]
    public async Task<IActionResult> AcceptRequest(Guid requestId)
    {
        var otherPlayerId = await _mediator.Send(new AcceptFriendRequestCommand(requestId, GetPlayerId()));
        if (otherPlayerId != null)
            await _hubContext.Clients.Group(otherPlayerId.ToString()!).SendAsync("FriendsUpdated");
        return Ok(new { success = otherPlayerId != null });
    }

    [HttpPost("decline/{requestId}")]
    public async Task<IActionResult> DeclineRequest(Guid requestId)
    {
        var otherPlayerId = await _mediator.Send(new DeclineFriendRequestCommand(requestId, GetPlayerId()));
        if (otherPlayerId != null)
            await _hubContext.Clients.Group(otherPlayerId.ToString()!).SendAsync("FriendsUpdated");
        return Ok(new { success = otherPlayerId != null });
    }

    [HttpDelete("{friendshipId}")]
    public async Task<IActionResult> RemoveFriend(Guid friendshipId)
    {
        var otherPlayerId = await _mediator.Send(new RemoveFriendCommand(friendshipId, GetPlayerId()));
        if (otherPlayerId != null)
            await _hubContext.Clients.Group(otherPlayerId.ToString()!).SendAsync("FriendsUpdated");
        return Ok(new { success = otherPlayerId != null });
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var result = await _mediator.Send(new GetFriendsListQuery(GetPlayerId()));
        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var result = await _mediator.Send(new GetPendingRequestsQuery(GetPlayerId()));
        return Ok(result);
    }
}
