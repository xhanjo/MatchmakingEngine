using MediatR;
using MatchmakingEngine.DTO;

namespace MatchmakingEngine.Application.Queries.Players;

public record GetAllPlayersQuery() : IRequest<List<PlayerResponseDto>>;
