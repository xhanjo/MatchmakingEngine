using MatchmakingEngine.Application.DTO;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.Matchmaking;

public record GetStatusQuery(Guid PlayerId) : IRequest<PollingStatusResponseDto>;