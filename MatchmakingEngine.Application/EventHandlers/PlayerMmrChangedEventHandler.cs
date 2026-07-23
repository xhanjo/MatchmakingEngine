using MatchmakingEngine.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MatchmakingEngine.Application.EventHandlers;

public class PlayerMmrChangedEventHandler : INotificationHandler<PlayerMmrChangedEvent>
{
    private readonly ILogger<PlayerMmrChangedEventHandler> _logger;

    public PlayerMmrChangedEventHandler(ILogger<PlayerMmrChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PlayerMmrChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DOMAIN EVENT] Player {PlayerId} changed MMR: {OldMmr} -> {NewMmr}", 
            notification.PlayerId, notification.OldMmr, notification.NewMmr);

        return Task.CompletedTask;
    }
}
