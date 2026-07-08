using Microsoft.AspNetCore.Mvc;
using MatchmakingEngine.Data;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Domain.Exceptions;

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

    [HttpGet]
    public async Task<IActionResult> GetAllPlayers()
    {
        var players = await _context.Players.ToListAsync();
        return Ok(players);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlayerById(Guid id)
    {
        var player = await _context.Players.FindAsync(id);

        if (player == null)
            throw new NotFoundException($"Player with ID {id} was not found");

        return Ok(player);
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