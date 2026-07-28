using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.DTO;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class CompleteMatchCommandHandler : IRequestHandler<CompleteMatchCommand, CompleteMatchResult>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteMatchCommandHandler> _logger;

    public CompleteMatchCommandHandler(
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        ILogger<CompleteMatchCommandHandler> logger)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CompleteMatchResult> Handle(CompleteMatchCommand request, CancellationToken cancellationToken)
    {

        var match = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: true);
        
        if (match == null)
            throw new NotFoundException($"Match with ID {request.MatchId} was not found.");

        if (match.Status != MatchStatus.Accepted)
            throw new ConflictException("Match has not started yet or is already finished.");

        if (request.WinnerId != match.Player1Id && request.WinnerId != match.Player2Id)
            throw new ConflictException("WinnerId must be one of the match participants");

        Guid loserId = (request.WinnerId == match.Player1Id) ? match.Player2Id : match.Player1Id;

        var winner = match.Player1Id == request.WinnerId ? match.Player1 : match.Player2;
        var loser = match.Player1Id == request.WinnerId ? match.Player2 : match.Player1;

        if (winner == null || loser == null)
            throw new NotFoundException("One or both players were not found in the database.");

        int MmrChange = 25;
        winner.RecordWin(MmrChange);
        loser.RecordLoss(MmrChange);

        match.Status = MatchStatus.Finished;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[GAME END] Match {matchId} completed. Winner: {winner} ({WMmr}), Loser: {loser} ({LMmr})",
            match.Id, winner.Username, winner.Mmr, loser.Username, loser.Mmr);

        return new CompleteMatchResult(
            "Match completed successfully!",
            new PlayerMatchResultDto(winner.Username, winner.Mmr),
            new PlayerMatchResultDto(loser.Username, loser.Mmr),
            match.Status
            );
    }
}
