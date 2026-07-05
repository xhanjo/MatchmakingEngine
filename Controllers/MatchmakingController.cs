using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using Microsoft.AspNetCore.Mvc;

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
        if (player == null) return NotFound(new { message = $"Player with ID {playerId} is not found"});

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
}
