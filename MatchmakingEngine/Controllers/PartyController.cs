using MatchmakingEngine.Application.Application.Commands.PartySystem;
using MatchmakingEngine.Application.Application.Queries.PartySystem;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MatchmakingEngine.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PartyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<MatchmakingHub> _hubContext;

    public PartyController(IMediator mediator, IHubContext<MatchmakingHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    private Guid GetPlayerId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateParty([FromBody] GameMode gameMode)
    {
        var result = await _mediator.Send(new CreatePartyCommand(GetPlayerId(), gameMode));
        return Ok(result);
    }

    [HttpPost("invite/{friendId}")]
    public async Task<IActionResult> InviteToParty(Guid friendId)
    {
        var playerId = GetPlayerId();

        var result = await _mediator.Send(new InviteToPartyCommand(playerId, friendId));

        await _hubContext.Clients.Group(friendId.ToString())
            .SendAsync("PartyInviteReceived", result.PartyId, playerId);

        return Ok(new { success = true });
    }

    [HttpPost("join/{partyId}")]
    public async Task<IActionResult> JoinParty(Guid partyId)
    {
        var result = await _mediator.Send(new JoinPartyCommand(GetPlayerId(), partyId));

        await _hubContext.Clients.Group(result.LeaderId.ToString())
            .SendAsync("PlayerJoinedParty", GetPlayerId());

        return Ok(result);
    }

    [HttpPost("leave")]
    public async Task<IActionResult> LeaveParty()
    {
        var disbanded = await _mediator.Send(new LeavePartyCommand(GetPlayerId()));
        return Ok(new { success = true, disbanded });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyParty()
    {
        var result = await _mediator.Send(new GetMyPartyQuery(GetPlayerId()));
        return Ok(result);
    }
}
