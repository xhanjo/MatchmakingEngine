using Microsoft.AspNetCore.Mvc;
using MatchmakingEngine.Data;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly MatchmakingDbContext _context;
    public PlayersController(MatchmakingDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterPlayerRequest request)
    {
        var player = new Player
        {
            Username = request.Username,
            Region = request.Region
        };

        _context.Players.Add(player);

        await _context.SaveChangesAsync();

        return StatusCode(201, player);
    }

}