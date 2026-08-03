using MatchmakingEngine.Application.Application.Queries.Players;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchmakingEngine.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaderboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int count = 100)
    {
        count = Math.Min(count, 1000);

        var query = new GetLeaderboardQuery(count);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
