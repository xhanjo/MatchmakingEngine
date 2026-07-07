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
    private readonly ILogger<MatchmakingController> _logger;
    public MatchmakingController(MatchmakingDbContext context, IMatchmakingQueue queue, ILogger<MatchmakingController> logger)
    {
        _context = context;
        _queue = queue;
        _logger = logger;
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
    [HttpPost("accept/{matchId}")]
    public async Task<IActionResult> AcceptMatch(Guid matchId, [FromQuery] Guid playerId)
    {
        var match = await _context.Matches.FindAsync(matchId);
        if (match == null) return NotFound(new { message = "Match not found!" });

        if (match.Status != MatchStatus.Pending)
            return BadRequest(new { message = "This match isn't waiting for acceptance anymore!" });

        if (playerId != match.Player1Id && playerId != match.Player2Id)
            return BadRequest(new { message = "You aren't a member of this match!" });

        if (playerId ==  match.Player1Id )
             match.Player1Accepted = true;
        if (playerId == match.Player2Id)
            match.Player2Accepted = true;

        if (match.Player1Accepted && match.Player2Accepted)
        {
            match.Status = MatchStatus.Accepted;
            _logger.LogInformation("[GAME START] All player accepted! Match {MatchId} is starting!", match.Id);
        }
        await _context.SaveChangesAsync();
        return Ok(new 
        { 
            status = match.Status == MatchStatus.Accepted ? "Match started" : "Waiting for other player",
            player1Accepted = match.Player1Accepted,
            player2Accepted = match.Player2Accepted,
            MatchStatus = match.Status
        });
    }
}
