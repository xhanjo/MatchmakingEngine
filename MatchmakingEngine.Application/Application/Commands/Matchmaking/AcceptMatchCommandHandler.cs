using MatchmakingEngine.Application.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.DTO;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class AcceptMatchCommandHandler : IRequestHandler<AcceptMatchCommand, AcceptMatchResult>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptMatchCommandHandler> _logger;
    private readonly IBackgroundJobService _backgroundJobService;

    public AcceptMatchCommandHandler(
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        ILogger<AcceptMatchCommandHandler> logger,
        IBackgroundJobService backgroundJobService)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<AcceptMatchResult> Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: true);

        if (match == null)
            throw new NotFoundException($"Match with ID {request.MatchId} was not found.");

        if (match.Status != MatchStatus.Pending)
            throw new ConflictException("This match is no longer waiting for acceptance.");

        var playerInMatch = match.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);

        if (playerInMatch == null)
            throw new ConflictException("You are not a participant in this match.");

        playerInMatch.Accepted = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var freshMatch = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: false, cancellationToken);
        bool allAccepted = freshMatch?.Players.All(p => p.Accepted) ?? false;

        if (allAccepted && match.Status == MatchStatus.Pending)
        {
            match.Status = MatchStatus.MapVeto;

            match.AvailableMaps = new List<string> { "Mirage", "Inferno", "Dust2", "Overpass", "Nuke", "Vertigo", "Ancient" };
            match.BannedMaps = new List<string>();

            match.CurrentVetoTurnPlayerId = match.Players.FirstOrDefault(p => p.Team == 1 && p.IsCaptain)?.PlayerId ?? match.Players.First().PlayerId;

            match.VetoDeadLine = DateTimeOffset.UtcNow.AddSeconds(30);

            _logger.LogInformation("[GAME START] All players accepted! Match {MatchId}  entering Map Veto phase. First turn: {PlayerId}", match.Id, match.CurrentVetoTurnPlayerId);
            var jobId = _backgroundJobService.Schedule<IMediator>(
                m => m.Publish(new MapVetoTimeoutEvent(match.Id, match.CurrentVetoTurnPlayerId.Value), CancellationToken.None),
                TimeSpan.FromSeconds(30));
            
            match.AssignVetoJobId(jobId);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        string statusMessage = match.Status == MatchStatus.Accepted ? "Match Started" : "Waiting for other player";

        return new AcceptMatchResult(
            statusMessage,
            allAccepted,
            match.Status,
            freshMatch?.Players.Select(p => p.PlayerId).ToList() ?? new List<Guid>(),
            match.Status == MatchStatus.MapVeto ? MatchmakingEngine.Application.DTO.VetoStateDto.FromMatch(match) : null
        );
    }
}
