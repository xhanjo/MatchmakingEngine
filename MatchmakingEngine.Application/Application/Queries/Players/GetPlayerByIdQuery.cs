using MatchmakingEngine.Application.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.Players;

public record GetPlayerByIdQuery(Guid Id) : IRequest<PlayerResponseDto>;
