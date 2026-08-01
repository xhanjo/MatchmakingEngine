using Microsoft.AspNetCore.Mvc;
using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Application.Queries.Players;
using MatchmakingEngine.Application.Commands.Players;
using Microsoft.AspNetCore.Authorization;

namespace MatchmakingEngine.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly IMediator _mediator;
    public PlayersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPlayers()
    {
        var result = await _mediator.Send(new GetAllPlayersQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlayerById(Guid id)
    {
        var result = await _mediator.Send(new GetPlayerByIdQuery(id));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterPlayerRequest request)
    {
        var command = new RegisterPlayerCommand(request.Username, request.Password, request.Region);
        var result = await _mediator.Send(command);
        return StatusCode(201, result);
    }

    [HttpGet("my/history")]
    public async Task<IActionResult> GetMyHistory()
    {
        var playerIdStr = User.FindFirst("PlayerId")?.Value;
        if (string.IsNullOrEmpty(playerIdStr))
            return Unauthorized();
            
        var playerId = Guid.Parse(playerIdStr);
        var result = await _mediator.Send(new MatchmakingEngine.Application.Application.Queries.MatchHistory.GetMatchHistoryQuery(playerId));
        return Ok(result);
    }
}