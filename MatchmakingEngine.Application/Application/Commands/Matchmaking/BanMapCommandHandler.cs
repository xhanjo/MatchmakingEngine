using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;
using MatchmakingEngine.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using MatchmakingEngine.Application.DTO;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class BanMapCommandHandler : IRequestHandler<BanMapCommand, BanMapResult>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BanMapCommandHandler> _logger;

    public BanMapCommandHandler(IMatchRepository matchRepository, IUnitOfWork unitOfWork, ILogger<BanMapCommandHandler> logger)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
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

        match.AvailableMaps.Remove(request.MapName);
        match.BannedMaps.Add(request.MapName);

        _logger.LogInformation("[MAP VETO] Player {PlayerId} banned map {MapName}. Maps left: {Count}",
            request.PlayerId, request.MapName, match.AvailableMaps.Count);

        if (match.AvailableMaps.Count == 1)
        {
            match.Status = Domain.MatchStatus.StartingServer;
            match.CurrentVetoTurnPlayerId = null;
            match.VetoDeadLine = null;

            _logger.LogInformation("[MAP VETO FINISHED] Chosen map: {MapName}. Starting server...",
                match.AvailableMaps[0]);
        }
        else
        {
            var otherPlayer = match.Players.FirstOrDefault(p => p.PlayerId != request.PlayerId);
            if (otherPlayer != null)
            {
                match.CurrentVetoTurnPlayerId = otherPlayer.PlayerId;
                match.VetoDeadLine = DateTimeOffset.UtcNow.AddSeconds(30);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BanMapResult(
            match.Players.Select(p => p.PlayerId).ToList(),
            request.MapName,
            match.AvailableMaps,
            match.CurrentVetoTurnPlayerId,
            match.Status
        );
    }
}
