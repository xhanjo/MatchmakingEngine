using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MatchmakingEngine.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Queries.Matchmaking;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Application.Application.Commands.Matchmaking;

namespace MatchmakingEngine.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatchmakingController : ControllerBase
{
    private readonly IMediator _mediator;
    public MatchmakingController(IMediator mediator)
    {
        _mediator = mediator;
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
