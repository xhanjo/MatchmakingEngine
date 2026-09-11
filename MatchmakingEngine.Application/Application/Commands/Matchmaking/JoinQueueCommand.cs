using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public record JoinQueueCommand(Guid PlayerId) : IRequest<bool>;