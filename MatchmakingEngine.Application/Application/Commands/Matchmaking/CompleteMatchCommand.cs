using MatchmakingEngine.Application.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public record CompleteMatchCommand(Guid MatchId) : IRequest<CompleteMatchResult>;
