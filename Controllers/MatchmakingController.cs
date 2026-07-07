using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.Net.NetworkInformation;

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
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId)
                && m.Status != MatchStatus.Finished
                && m.Status != MatchStatus.Canceled)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (match != null)
        {
            return Ok(new PollingStatusResponseDto(
                Status: PollingStatus.MatchFound.ToString(),
                LobbyId: match.Id,
                AverageMmr: match.AverageMmr,
                CreatedAt: match.CreatedAt
            ));
        }

        if (_queue.IsPlayerInQueue(playerId))
        {
            return Ok(new PollingStatusResponseDto(
                Status: PollingStatus.Searching.ToString()
                ));
        }

        return Ok(new PollingStatusResponseDto(
            Status: PollingStatus.Idle.ToString()
            ));
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

        if (playerId == match.Player1Id)
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

    [HttpPost("complete/{matchId}")]
    public async Task<IActionResult> CompleteMatch(Guid matchId, [FromQuery] Guid winnerId)
    {

        var match = await _context.Matches.FindAsync(matchId);
        if (match == null)
            return NotFound(new { message = "Match not found!" });

        if (match.Status != MatchStatus.Accepted)
            return BadRequest(new { message = "Match hasn't started yet or already has ended" });

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
        {
            return BadRequest(new { message = "Winner wasn't a member of that match!" });
        }

        Guid loserId = (winnerId == match.Player1Id) ? match.Player2Id : match.Player1Id;

        var winner = await _context.Players.FindAsync(winnerId);
        var loser = await _context.Players.FindAsync(loserId);

        if (winner == null || loser == null)
            return NotFound(new { message = "One of the players wasn't found in the database!" });

        double MmrChange = 30.0;

        winner.RecordWin(MmrChange);
loser.RecordLoss(MmrChange);
        match.Status = MatchStatus.Finished;

        await _context.SaveChangesAsync();

        _logger.LogInformation("[GAME END] Match {matchId} completed! Winner: {winner} ({WMmr}), Loser: {loser} ({LMmr})",
            match.Id, winner.Username, winner.Mmr, loser.Username, loser.Mmr);

        return Ok(new
        {
            message = "Match completed successfully!",
            winner = new { winner.Username, newMmr = winner.Mmr },
            loser = new { loser.Username, newMmr = loser.Mmr },
            MatchStatus = match.Status
        });
    }
}
