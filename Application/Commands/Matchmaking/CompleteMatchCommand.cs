using MatchmakingEngine.Domain;
using MediatR;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public record PlayerMatchResultDto(string Username, double NewMmr);

public record CompleteMatchResult(
    string Message,
    PlayerMatchResultDto Winner,
    PlayerMatchResultDto Loser,
    MatchStatus MatchStatus
    );
public record CompleteMatchCommand(Guid MatchId, Guid WinnerId) : IRequest<CompleteMatchResult>;
