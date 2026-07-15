using MediatR;
using MatchmakingEngine.Data;
using MatchmakingEngine.Services;
using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Domain.Exceptions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class AcceptMatchCommandHandler : IRequestHandler<AcceptMatchCommand, AcceptMatchResult>
{
    private readonly MatchmakingDbContext _context;
    private readonly ILogger<AcceptMatchCommandHandler> _logger;

    public AcceptMatchCommandHandler(MatchmakingDbContext context, ILogger<AcceptMatchCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AcceptMatchResult> Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await _context.Matches.FindAsync(new object[] { request.MatchId }, cancellationToken);
        if (match == null)
            throw new NotFoundException($"Match with ID {request.MatchId} was not found.");

        if (match.Status != MatchStatus.Pending)
            throw new ConflictException("This match is no longer waiting for acceptance.");

        if (request.PlayerId != match.Player1Id && request.PlayerId != match.Player2Id)
            throw new ConflictException("You are not a participant in this match.");

        if (request.PlayerId == match.Player1Id)
            match.Player1Accepted = true;
        if (request.PlayerId == match.Player2Id)
            match.Player2Accepted = true;

        if (match.Player1Accepted && match.Player2Accepted)
        {
            match.Status = MatchStatus.Accepted;
            _logger.LogInformation("[GAME START] All player accepted! Match {MatchId} is starting!", match.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        string statusMesage = match.Status == MatchStatus.Accepted ? "Match Started" : "Waiting for other player";

        return new AcceptMatchResult(
            statusMesage,
            match.Player1Accepted,
            match.Player2Accepted,
            match.Status
            );
    }
}
