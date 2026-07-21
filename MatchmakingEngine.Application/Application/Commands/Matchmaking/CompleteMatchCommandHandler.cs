using MediatR;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.DTO;
using Microsoft.Extensions.Logging;
using MatchmakingEngine.Application.Interfaces;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class CompleteMatchCommandHandler : IRequestHandler<CompleteMatchCommand, CompleteMatchResult>
{
    private readonly IMatchmakingDbContext _context;
    private readonly ILogger<CompleteMatchCommandHandler> _logger;

    public CompleteMatchCommandHandler(IMatchmakingDbContext context, ILogger<CompleteMatchCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CompleteMatchResult> Handle(CompleteMatchCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var match = await _context.Matches.FindAsync(new object[] { request.MatchId }, cancellationToken);
            if (match == null)
                throw new NotFoundException($"Match with ID {request.MatchId} was not found.");

            if (match.Status != MatchStatus.Accepted)
                throw new ConflictException("Match has not starter yet or is already finished.");

            Guid loserId = (request.WinnerId == match.Player1Id) ? match.Player2Id : match.Player1Id;

            var winner = await _context.Players.FindAsync(new object[] { request.WinnerId }, cancellationToken);
            var loser = await _context.Players.FindAsync(new object[] { loserId }, cancellationToken);

            if (winner == null || loser == null)
                throw new NotFoundException("One or both players were not found in the database.");

            int MmrChange = 25;
            winner.RecordWin(MmrChange);
            loser.RecordLoss(MmrChange);

            match.Status = MatchStatus.Finished;

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("[GAME END] Match {matchId} completed. Winner: {winner} ({WMmr}), Loser: {loser} ({LMmr})",
                match.Id, winner.Username, winner.Mmr, loser.Username, loser.Mmr);

            return new CompleteMatchResult(
                "Match completed successfuly!",
                new PlayerMatchResultDto(winner.Username, winner.Mmr),
                new PlayerMatchResultDto(loser.Username, loser.Mmr),
                match.Status
                );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

    }
}
