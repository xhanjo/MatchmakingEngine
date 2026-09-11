using MatchmakingEngine.Application.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;
public record AcceptMatchCommand(Guid PlayerId, Guid MatchId) : IRequest<AcceptMatchResult>;
