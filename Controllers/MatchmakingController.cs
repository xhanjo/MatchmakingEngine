using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.Net.NetworkInformation;
using MatchmakingEngine.Hubs;

namespace MatchmakingEngine.Controllers;

[Authorize]
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

    [HttpPost("join")]
    public async Task<IActionResult> JoinQueue()
    {
        var playerIdStr = User.FindFirst("PlayerId")?.Value;

        if (string.IsNullOrEmpty(playerIdStr))
            return Unauthorized("Invalid token: PlayerId claim is missing");

        var playerId = Guid.Parse(playerIdStr);

        var player = await _context.Players.FindAsync(playerId);

        if (player == null)
            throw new NotFoundException($"Player with ID {playerId} was not found in database.");

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

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var playerIdStr = User.FindFirst("PlayerId")?.Value;
        
        if (string.IsNullOrEmpty(playerIdStr))
            return Unauthorized("Invalid token: PlayerId claim is missing");

        var playerId = Guid.Parse(playerIdStr);

        var player = await _context.Players.FindAsync(playerId);

        if (player == null)
            throw new NotFoundException($"Player with ID {playerId} was not found in database.");

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
    public async Task<IActionResult> AcceptMatch(Guid matchId)
    {
        var playerIdStr = User.FindFirst("PlayerId")?.Value;

        if (string.IsNullOrEmpty(playerIdStr))
            return Unauthorized("Invalid token: PlayerId claim is missing");

        var playerId = Guid.Parse(playerIdStr);

        var match = await _context.Matches.FindAsync(matchId);
        if (match == null)
            throw new NotFoundException($"Match with ID {matchId} was not found.");

        if (match.Status != MatchStatus.Pending)
            throw new ConflictException("This match is no longer waiting for acceptance.");

        if (playerId != match.Player1Id && playerId != match.Player2Id)
            throw new ConflictException("You are not a participant in this match.");

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
            throw new NotFoundException($"Match with ID {matchId} was not found.");

        if (match.Status != MatchStatus.Accepted)
            throw new ConflictException("Match has not started yet or is already finished");

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
        {
            throw new ConflictException("The winner was not a participant in this match.");
        }

        Guid loserId = (winnerId == match.Player1Id) ? match.Player2Id : match.Player1Id;

        var winner = await _context.Players.FindAsync(winnerId);
        var loser = await _context.Players.FindAsync(loserId);

        if (winner == null || loser == null)
            throw new NotFoundException("One or both players were not found in the database.");

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
