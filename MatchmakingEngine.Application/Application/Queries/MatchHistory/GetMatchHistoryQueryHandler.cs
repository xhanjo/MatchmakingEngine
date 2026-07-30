using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.MatchHistory;

public class GetMatchHistoryQuery : IRequest<List<MatchDto>>
{
    public Guid PlayerId { get; }
    public GetMatchHistoryQuery(Guid playerID) => PlayerId = playerID;
}

public class GetMatchHistoryQueryHandler : IRequestHandler<GetMatchHistoryQuery, List<MatchDto>>
{
    private readonly IMatchRepository _matchRepository;

    public GetMatchHistoryQueryHandler(IMatchRepository matchRepository)
    {
        _matchRepository = matchRepository;
    }
    public async Task<List<MatchDto>> Handle(GetMatchHistoryQuery request, CancellationToken cancellationToken)
    {
        var matches = await _matchRepository.GetMatchHistoryByPlayerIdAsync(request.PlayerId, trackChanges: false, cancellationToken);

        return matches.Select(m => new MatchDto(
            m.Id,
            m.Status,
            m.CreatedAt,
            m.GameMode,
            m.Players.Select(p => new MatchPlayerDto(
               p.PlayerId,
               p.Player.Username,
               p.Accepted,
               p.Kills,
               p.Deaths,
               p.Assists,
               p.Score,
               p.IsMvp
             )).ToList()
            )).ToList();
    }
}
