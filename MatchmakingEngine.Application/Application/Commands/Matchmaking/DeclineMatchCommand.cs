using System;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public record DeclineMatchCommand(Guid MatchId, Guid PlayerId) : IRequest<bool>;
