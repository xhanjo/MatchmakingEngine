using MediatR;

namespace MatchmakingEngine.Application.Commands.Auth;

public record LoginCommand(string Username, string Password) : IRequest<string>;
