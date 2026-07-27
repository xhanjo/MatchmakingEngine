using MatchmakingEngine.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchmakingEngine.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IMatchRepository _matchRepository;

    public AdminController(IMatchRepository matchRepository)
    {
        _matchRepository = matchRepository;
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetAllMatches()
    {
        var matches = await _matchRepository.GetAllMatchesAsync();

        var result = matches.Select(m => new
        {
            Id = m.Id,
            Status = m.Status.ToString(),
            AverageMmr = m.AverageMmr
        });

        return Ok(result);
    }
}
