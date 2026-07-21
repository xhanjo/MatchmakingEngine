using MediatR;
using MatchmakingEngine.Domain;
using MatchmakingEngine.DTO;

namespace MatchmakingEngine.Application.Commands.Matchmaking;
public record AcceptMatchCommand(Guid PlayerId, Guid MatchId) : IRequest<AcceptMatchResult>;
