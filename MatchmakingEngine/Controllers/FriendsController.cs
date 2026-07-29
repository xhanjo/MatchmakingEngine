using MatchmakingEngine.Application.Application.Commands.Friends;
using MatchmakingEngine.Application.Application.Queries.Friends;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MatchmakingEngine.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FriendsController(IMediator mediator)
    {
        _mediator = mediator;
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
        return Ok(new { success = result });
    }

    [HttpPost("accept/{requestId}")]
    public async Task<IActionResult> AcceptRequest(Guid requestId)
    {
        var result = await _mediator.Send(new AcceptFriendRequestCommand(requestId, GetPlayerId()));
        return Ok(new { success = result });
    }

    [HttpPost("decline/{requestId}")]
    public async Task<IActionResult> DeclineRequest(Guid requestId)
    {
        var result = await _mediator.Send(new DeclineFriendRequestCommand(requestId, GetPlayerId()));
        return Ok(new { success = result });
    }

    [HttpDelete("{friendshipId}")]
    public async Task<IActionResult> RemoveFriend(Guid friendshipId)
    {
        var result = await _mediator.Send(new RemoveFriendCommand(friendshipId, GetPlayerId()));
        return Ok(new { success = result });
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
