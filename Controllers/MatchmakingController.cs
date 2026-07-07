using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchmakingController : ControllerBase
{
    private readonly MatchmakingDbContext _context;
    private readonly IMatchmakingQueue _queue;

    public MatchmakingController(MatchmakingDbContext context, IMatchmakingQueue queue)
    {
        _context = context;
        _queue = queue;
    }

    [HttpPost("join/{playerId}")]
    public async Task<IActionResult> JoinQueue(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { message = $"Player with ID {playerId} is not found" });

        var ticket = new MatchmakingTicket(
            TicketId: Guid.NewGuid(),
            PlayerId: player.Id,
            Username: player.Username,
            Mmr: player.Mmr,
            TrustFactor: player.TrustFactor,
            Region: player.Region,
            EnqueuedAt: DateTimeOffset.UtcNow
            );

        await _queue.EnqueueAsync(ticket);

        return Ok(new { message = "Player added to search queue!", ticket });
    }

    [HttpGet("status/{playerId}")]
    public async Task<IActionResult> GetStatus(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { message = "Player don't exist!" });

        var match = await _context.Matches
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (match != null)
        {
            return Ok(new 
            { 
                status = "MatchFound", 
                lobbyId = match.Id, 
                AverageMmr = match.AverageMmr, 
                CreatedAt = match.CreatedAt 
            });
        }

        if (_queue.IsPlayerInQueue(playerId))
        {
            return Ok(new { status = "Searching" });
        }

        return Ok(new { status = "Idle" });
    }
}
