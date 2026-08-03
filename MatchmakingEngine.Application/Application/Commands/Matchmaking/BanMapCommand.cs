using MatchmakingEngine.Application.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public record BanMapCommand(Guid MatchId, Guid PlayerId, string MapName) : IRequest<BanMapResult>;