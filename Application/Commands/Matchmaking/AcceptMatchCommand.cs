using MediatR;
using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public record AcceptMatchResult(
    string StatusMessage,
    bool Player1Accepted,
    bool Player2Accepted,
    MatchStatus MatchStatus
    );
public record AcceptMatchCommand(Guid PlayerId, Guid MatchId) : IRequest<AcceptMatchResult>;
