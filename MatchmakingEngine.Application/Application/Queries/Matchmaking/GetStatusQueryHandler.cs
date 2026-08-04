using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;

namespace MatchmakingEngine.Application.Queries.Matchmaking;

public class GetStatusQueryHandler : IRequestHandler<GetStatusQuery, PollingStatusResponseDto>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IMatchmakingQueue _queue;
    private readonly IPartyRepository _partyRepository;

    public GetStatusQueryHandler(IMatchRepository matchRepository, IMatchmakingQueue queue, IPartyRepository partyRepository)
    {
        _matchRepository = matchRepository;
        _queue = queue;   
        _partyRepository = partyRepository;
    }

    public async Task<PollingStatusResponseDto> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetActiveMatchByPlayerIdAsync(request.PlayerId, trackChanges: true);
    
        if (match != null)
        {
            string statusStr;
            MatchmakingEngine.Application.DTO.VetoStateDto? vetoState = null;

            if (match.Status == MatchStatus.Pending)
            {
                statusStr = PollingStatus.MatchFound.ToString();
            }
            else if (match.Status == MatchStatus.MapVeto || match.Status == MatchStatus.StartingServer)
            {
                statusStr = "MapVeto";
                vetoState = MatchmakingEngine.Application.DTO.VetoStateDto.FromMatch(match);
            }
            else
            {
                statusStr = PollingStatus.InGame.ToString();
            }

            return new PollingStatusResponseDto(
                statusStr,
                match.Id,
                match.AverageMmr,
                match.CreatedAt,
                vetoState
            );
        }

        if (await _queue.IsPlayerInQueueAsync(request.PlayerId))
        {
            return new PollingStatusResponseDto(PollingStatus.Searching.ToString());
        }

        var party = await _partyRepository.GetPartyByPlayerIdAsync(request.PlayerId, trackChanges: false, cancellationToken);
        if (party != null && party.LeaderId != request.PlayerId)
        {
            if (await _queue.IsPlayerInQueueAsync(party.LeaderId))
            {
                return new PollingStatusResponseDto(PollingStatus.Searching.ToString());
            }
        }

        return new PollingStatusResponseDto(
            PollingStatus.Idle.ToString()
            );
    }
}
