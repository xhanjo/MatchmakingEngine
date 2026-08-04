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

        if (result.AllAccepted && result.PlayerIds != null)
        {
            foreach (var pid in result.PlayerIds)
            {
                if (result.MatchStatus == Domain.MatchStatus.MapVeto)
                    await _hubContext.Clients.Group(pid.ToString()).SendAsync("MatchReadyForVeto", result.VetoState);
                else
                    await _hubContext.Clients.Group(pid.ToString()).SendAsync("MatchStarted", matchId);
            }
            await _hubContext.Clients.Group("Admins").SendAsync("AdminMatchesUpdated");
        }

        return Ok(result);
    }

    [HttpPost("decline/{matchId}")]
    public async Task<IActionResult> DeclineMatch(Guid matchId)
    {
        var playerIdStr = User.FindFirst("PlayerId")?.Value;
        if (!Guid.TryParse(playerIdStr, out var playerId))
            return Unauthorized();

        var playerIds = await _mediator.Send(new DeclineMatchCommand(matchId, playerId));
        if (playerIds == null || !playerIds.Any())
            return BadRequest("Cannot decline match.");

        foreach (var pid in playerIds)
        {
            await _hubContext.Clients.Group(pid.ToString()).SendAsync("MatchCanceled");
        }
        await _hubContext.Clients.Group("Admins").SendAsync("AdminMatchesUpdated");
        
        return Ok();
    }

    [HttpPost("veto/{matchId}/{mapName}")]
    public async Task<IActionResult> VetoMap(Guid matchId, string mapName)
    {
        var playerId = GetPlayerIdFromToken();
        var result = await _mediator.Send(new BanMapCommand(matchId, playerId, mapName));

        foreach (var pid in result.PlayerIds)
        {
            await _hubContext.Clients.Group(pid.ToString()).SendAsync("MapVetoUpdated", result.VetoState);
        }

        if (result.VetoState.Status == "Completed")
        {
            var chosenMap = result.VetoState.Maps.FirstOrDefault(m => !m.IsBanned)?.Name;
            foreach (var pid in result.PlayerIds)
            {
                await _hubContext.Clients.Group(pid.ToString()).SendAsync("MatchStarting", chosenMap);
            }
        }
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("complete/{matchId}")]
    public async Task<IActionResult> CompleteMatch(Guid matchId)
    {
        var result = await _mediator.Send(new CompleteMatchCommand(matchId));

        foreach(var player in result.Scoreboard)
        {
            await _hubContext.Clients.Group(player.PlayerId.ToString()).SendAsync("MatchFinished", result);
        }

        await _hubContext.Clients.Group("Admins").SendAsync("AdminMatchesUpdated");

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
