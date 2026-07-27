using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public record LeaveQueueCommand(Guid PlayerId) : IRequest<bool>;
