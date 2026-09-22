using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.MatchHistory;

public class GetMatchHistoryQuery : IRequest<List<MatchDto>>
{
    public Guid PlayerId { get; }
    public GetMatchHistoryQuery(Guid playerId) => PlayerId = playerId;
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

                var team1Players = m.Players.Where(p => p.Team == 1).ToList();
                var team2Players = m.Players.Where(p => p.Team == 2).ToList();
                var team1AvgMmr = team1Players.Any() ? team1Players.Average(p => p.Player != null ? p.Player.Mmr : 1000) : 1000;
                var team2AvgMmr = team2Players.Any() ? team2Players.Average(p => p.Player != null ? p.Player.Mmr : 1000) : 1000;

                var expectedScore1 = 1.0 / (1.0 + Math.Pow(10.0, (team2AvgMmr - team1AvgMmr) / 400.0));
                var expectedScore2 = 1.0 / (1.0 + Math.Pow(10.0, (team1AvgMmr - team2AvgMmr) / 400.0));

                int kFactor = 50;
                int team1MmrChange = (int)Math.Round(kFactor * ((winningTeam == 1 ? 1.0 : 0.0) - expectedScore1));
                int team2MmrChange = (int)Math.Round(kFactor * ((winningTeam == 2 ? 1.0 : 0.0) - expectedScore2));

                return new MatchDto(
                    m.Id,
                    m.Status,
                    m.CreatedAt,
                    m.GameMode,
                    m.SelectedMap ?? (m.AvailableMaps.Count == 1 ? m.AvailableMaps[0] : null),
                    m.Players.Select(p => {
                        var change = p.MmrChange != 0 ? p.MmrChange : (p.Team == 1 ? team1MmrChange : team2MmrChange);
                        return new MatchPlayerDto(
                           p.PlayerId,
                           p.Player?.Username ?? "Unknown",
                           p.Accepted,
                           p.Kills,
                           p.Deaths,
                           p.Assists,
                           p.Score,
                           p.IsMvp,
                           p.Team,
                           p.Team == winningTeam,
                           p.Player?.Mmr ?? 1000,
                           change
                        );
                    }).ToList()
                );
            }).ToList();
    }
}
