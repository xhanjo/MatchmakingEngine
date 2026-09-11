using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class BanMapCommandHandler : IRequestHandler<BanMapCommand, BanMapResult>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BanMapCommandHandler> _logger;
    private readonly IBackgroundJobService _backgroundJobService;
    public BanMapCommandHandler(IMatchRepository matchRepository, IUnitOfWork unitOfWork, ILogger<BanMapCommandHandler> logger, IBackgroundJobService backgroundJobService)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<BanMapResult> Handle(BanMapCommand request, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: true);

        if (match == null)
            throw new NotFoundException("Match not found.");

        if (match.Status != Domain.MatchStatus.MapVeto)
            throw new ConflictException("Match is not in map Veto phase.");

        if (match.CurrentVetoTurnPlayerId != request.PlayerId)
            throw new ConflictException("It is not your turn to ban a map.");

        if (!match.AvailableMaps.Contains(request.MapName))
            throw new ConflictException($"Map {request.MapName} is not available or already banned");

        if (match.AvailableMaps.Count == 1)
            throw new ConflictException("Cannot ban the last remaining map. This is the chosen map!");

        match.AvailableMaps.Remove(request.MapName);
        match.BannedMaps.Add(request.MapName);

        if (!string.IsNullOrEmpty(match.CurrentVetoJobId))
        {
            _backgroundJobService.Delete(match.CurrentVetoJobId);
        }

        _logger.LogInformation("[MAP VETO] Player {PlayerId} banned map {MapName}. Maps left: {Count}",
            request.PlayerId, request.MapName, match.AvailableMaps.Count);

        if (match.AvailableMaps.Count == 1)
        {
            match.Status = Domain.MatchStatus.StartingServer;
            match.SelectedMap = match.AvailableMaps[0];
            match.CurrentVetoTurnPlayerId = null;
            match.VetoDeadLine = null;
            match.ClearVetoJobId();

            _logger.LogInformation("[MAP VETO FINISHED] Chosen map: {MapName}. Starting server...",
                match.AvailableMaps[0]);
        }
        else
        {
            var team1Captain = match.Players.FirstOrDefault(p => p.Team == 1 && p.IsCaptain)?.PlayerId;
            var team2Captain = match.Players.FirstOrDefault(p => p.Team == 2 && p.IsCaptain)?.PlayerId;

            match.CurrentVetoTurnPlayerId = (request.PlayerId == team1Captain) ? team2Captain : team1Captain;
            match.VetoDeadLine = DateTimeOffset.UtcNow.AddSeconds(30);

            var jobId = _backgroundJobService.Schedule<IMediator>(
                m => m.Publish(new MapVetoTimeoutEvent(match.Id, match.CurrentVetoTurnPlayerId!.Value), CancellationToken.None),
                TimeSpan.FromSeconds(30));
                
            match.AssignVetoJobId(jobId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BanMapResult(
            match.Players.Select(p => p.PlayerId).ToList(),
            VetoStateDto.FromMatch(match)
        );
    }
}
