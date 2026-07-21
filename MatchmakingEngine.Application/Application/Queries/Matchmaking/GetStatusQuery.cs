using MediatR;
using MatchmakingEngine.DTO;

namespace MatchmakingEngine.Application.Queries.Matchmaking;

public record GetStatusQuery(Guid PlayerId) : IRequest<PollingStatusResponseDto>;