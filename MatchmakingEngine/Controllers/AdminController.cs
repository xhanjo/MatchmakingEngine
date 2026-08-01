using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
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
            AverageMmr = m.AverageMmr,
            GameMode = m.GameMode.ToString(),
            CreatedAt = m.CreatedAt,
            PlayerCount = m.Players?.Count ?? 0
        });

        return Ok(result);
    }

    [HttpGet("active-matches")]
    public async Task<IActionResult> GetActiveMatches()
    {
        var matches = await _matchRepository.GetAllMatchesAsync();
        var active = matches.Where(m => m.Status == MatchStatus.Pending || m.Status == MatchStatus.Accepted);
        var result = active.Select(m => new
        {
            Id = m.Id,
            Status = m.Status.ToString(),
            AverageMmr = m.AverageMmr,
            GameMode = m.GameMode.ToString(),
            CreatedAt = m.CreatedAt,
            PlayerCount = m.Players?.Count ?? 0
        });
        return Ok(result);
    }
}
