using MatchmakingEngine.Domain;
using MatchmakingEngine.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public record CompleteMatchCommand(Guid MatchId, Guid WinnerId) : IRequest<CompleteMatchResult>;
