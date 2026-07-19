using MediatR;
using MatchmakingEngine.DTO;

namespace MatchmakingEngine.Application.Queries.Players;

public record GetPlayerByIdQuery(Guid Id) : IRequest<PlayerResponseDto>;
