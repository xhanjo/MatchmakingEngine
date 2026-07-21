using MediatR;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public record JoinQueueCommand(Guid PlayerId) : IRequest<bool>;