using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Commands.Players;

public record RegisterPlayerCommand(string Username, string Password, PlayerRegion Region) : IRequest<PlayerResponseDto>;
