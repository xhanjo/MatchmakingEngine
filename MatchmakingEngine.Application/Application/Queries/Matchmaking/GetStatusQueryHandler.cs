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

    public GetStatusQueryHandler(IMatchRepository matchRepository, IMatchmakingQueue queue)
    {
        _matchRepository = matchRepository;
        _queue = queue;   
    }

    public async Task<PollingStatusResponseDto> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetActiveMatchByPlayerIdAsync(request.PlayerId, trackChanges: false);
    
        if (match != null)
        {
            string statusStr = match.Status == MatchStatus.Pending 
                ? PollingStatus.MatchFound.ToString() 
                : PollingStatus.InGame.ToString();

            return new PollingStatusResponseDto(
                statusStr,
                match.Id,
                match.AverageMmr,
                match.CreatedAt
                );
        }

        if (await _queue.IsPlayerInQueueAsync(request.PlayerId))
        {
            return new PollingStatusResponseDto(
                PollingStatus.Searching.ToString()
                );
        }

        return new PollingStatusResponseDto(
            PollingStatus.Idle.ToString()
            );
    }
}
