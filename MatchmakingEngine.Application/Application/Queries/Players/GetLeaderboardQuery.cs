using MatchmakingEngine.Application.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.Players;

public record GetLeaderboardQuery(int Count = 100) : IRequest<List<LeaderboardEntryDto>>;
