using MatchmakingEngine.Application.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace MatchmakingEngine.Events;

public class MapVetoTimeoutEventHandler : INotificationHandler<MapVetoTimeoutEvent>
{
    private readonly IMatchRepository _matchRepo;
    private readonly IMediator _mediator;
    private readonly IHubContext<MatchmakingHub> _hubContext;

    public MapVetoTimeoutEventHandler(IMatchRepository matchRepo, IMediator mediator, IHubContext<MatchmakingHub> hubContext)
    {
        _matchRepo = matchRepo;
        _mediator = mediator;
        _hubContext = hubContext;
    }

    public async Task Handle(MapVetoTimeoutEvent notification, CancellationToken cancellationToken)
    {
        var match = await _matchRepo.GetByIdWithPlayersAsync(notification.MatchId, trackChanges: false, cancellationToken);
        if (match == null || match.Status != MatchmakingEngine.Domain.MatchStatus.MapVeto) return;

        if (match.AvailableMaps.Any() && match.CurrentVetoTurnPlayerId.HasValue)
        {
            var randomMap = match.AvailableMaps[new Random().Next(match.AvailableMaps.Count)];
            var result = await _mediator.Send(new BanMapCommand(match.Id, match.CurrentVetoTurnPlayerId.Value, randomMap), cancellationToken);

            foreach (var pid in result.PlayerIds)
            {
                await _hubContext.Clients.Group(pid.ToString()).SendAsync("MapVetoUpdated", result.VetoState, cancellationToken: cancellationToken);
            }
        }
    }
}