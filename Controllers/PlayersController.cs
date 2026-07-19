using Microsoft.AspNetCore.Mvc;
using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Application.Queries.Players;
using MatchmakingEngine.Application.Commands.Players;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;

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

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAllPlayers()
    {
        var result = await _mediator.Send(new GetAllPlayersQuery());
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlayerById(Guid id)
    {
        var result = await _mediator.Send(new GetPlayerByIdQuery(id));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterPlayerRequest request)
    {
        var command = new RegisterPlayerCommand(request.Username, request.Password, request.Region);
        var result = await _mediator.Send(command);
        return StatusCode(201, result);
    }

}