using MatchmakingEngine.Application.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Auth;

public record RefreshCommand(string RefreshToken) : IRequest<AuthResult>;