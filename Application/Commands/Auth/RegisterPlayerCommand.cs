using MatchmakingEngine.Domain;
using MediatR;

namespace MatchmakingEngine.Application.Commands.Auth;

public record RegisterPlayerCommand(string Username, string Password, PlayerRegion Region) : IRequest<Guid>;

