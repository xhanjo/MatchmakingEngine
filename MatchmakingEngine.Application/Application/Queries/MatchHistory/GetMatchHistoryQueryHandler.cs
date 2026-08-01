using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
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

        return matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Select(m => {
                var t1Kills = m.Players.Where(x => x.Team == 1).Sum(x => x.Kills);
                var t2Kills = m.Players.Where(x => x.Team == 2).Sum(x => x.Kills);
                var winningTeam = t1Kills > t2Kills ? 1 : (t2Kills > t1Kills ? 2 : 1);

                return new MatchDto(
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
                       p.IsMvp,
                       p.Team,
                       p.Team == winningTeam
                     )).ToList()
                );
            }).ToList();
    }
}
