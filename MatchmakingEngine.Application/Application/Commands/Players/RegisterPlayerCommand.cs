using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Domain;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Players;

public record RegisterPlayerCommand(string Username, string Password, PlayerRegion Region) : IRequest<PlayerResponseDto>;
