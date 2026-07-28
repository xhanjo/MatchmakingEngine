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

        match.Status = MatchStatus.Finished;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompleteMatchResult(
            "Match completed",
            Guid.Empty,
            Guid.Empty,
            "Temp",
            new List<PlayerStatsDto>(),
            match.Status
        );
    }
}

