using MatchmakingEngine.Application.Interfaces;
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
        var matches = await _matchRepository.GetMatchHistoryByPlayerIdAsync(request.PlayerId, trackChanges: false);

        var dtos = matches.Select(m => new MatchDto(
                m.Id,
                m.Status.ToString(),
                new PlayerDto(m.Player1Id, m.Player1.Username, m.Player1.Mmr),
                new PlayerDto(m.Player2Id, m.Player2.Username, m.Player2.Mmr)
            )).ToList();

        return dtos;
    }
}
