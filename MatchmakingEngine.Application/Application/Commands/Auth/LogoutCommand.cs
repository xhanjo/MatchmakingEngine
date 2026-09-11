using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Auth;

public record LogoutCommand(string RefreshToken) : IRequest;