using MatchmakingEngine.Application.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Queries.Matchmaking;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MatchmakingEngine.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatchmakingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<MatchmakingHub> _hubContext;

    public MatchmakingController(IMediator mediator, IHubContext<MatchmakingHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinQueue()
    {
        var playerId = GetPlayerIdFromToken();
        var success = await _mediator.Send(new JoinQueueCommand(playerId));

        if (!success) return NotFound(new { message = "Player not found in database." });

        return Ok(new { message = "Player added to search queue!"});
    }

    [HttpPost("leave")]
    public async Task<IActionResult> LeaveQueue()
    {
        var playerId = GetPlayerIdFromToken();
        var success = await _mediator.Send(new LeaveQueueCommand(playerId));

        if (!success)
            return BadRequest(new { message = "Player is not in the queue." });

        return Ok(new { message = "Player removed from search queue." });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var playerId = GetPlayerIdFromToken();
        var result = await _mediator.Send(new GetStatusQuery(playerId));

        return Ok(result);
    }

    [HttpPost("accept/{matchId}")]
    public async Task<IActionResult> AcceptMatch(Guid matchId)
    {
        var playerId = GetPlayerIdFromToken();
        var result = await _mediator.Send(new AcceptMatchCommand(playerId, matchId));

        return Ok(result);
    }

    [HttpPost("decline/{matchId}")]
    public async Task<IActionResult> DeclineMatch(Guid matchId)
    {
        var playerIdStr = User.FindFirst("PlayerId")?.Value;
        if (!Guid.TryParse(playerIdStr, out var playerId))
            return Unauthorized();

        var result = await _mediator.Send(new DeclineMatchCommand(matchId, playerId));
        if (!result)
            return BadRequest("Cannot decline match.");

        await _hubContext.Clients.Group(matchId.ToString()).SendAsync("MatchCanceled");
        
        return Ok();
    }


    [Authorize(Roles = "Admin")]
    [HttpPost("complete/{matchId}")]
    public async Task<IActionResult> CompleteMatch(Guid matchId, [FromQuery] Guid winnerId)
    {
        var result = await _mediator.Send(new CompleteMatchCommand(matchId, winnerId));

        return Ok(result);
    }

    private Guid GetPlayerIdFromToken()
    {
        var playerIdStr = User.FindFirst("PlayerId")?.Value;

        if (string.IsNullOrEmpty(playerIdStr))
            throw new UnauthorizedAccessException("Invalid token: PlayerId claim is missing");

        return Guid.Parse(playerIdStr);
    }
}
