using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Auth;

public record LoginCommand(string Username, string Password) : IRequest<string>;
