using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public record MapVetoTimeoutEvent(Guid MatchId, Guid ExpectedTurnPlayerId) : INotification;