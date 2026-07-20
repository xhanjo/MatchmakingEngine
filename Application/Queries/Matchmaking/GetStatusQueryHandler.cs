using MediatR;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Data;
using MatchmakingEngine.Services;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Application.Queries.Matchmaking;

public class GetStatusQueryHandler : IRequestHandler<GetStatusQuery, PollingStatusResponseDto>
{
    private readonly MatchmakingDbContext _context;
    private readonly IMatchmakingQueue _queue;

    public GetStatusQueryHandler(MatchmakingDbContext context, IMatchmakingQueue queue)
    {
        _context = context;
        _queue = queue;   
    }

    public async Task<PollingStatusResponseDto> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var match = await _context.Matches
            .AsNoTracking()
            .Where(m => (m.Player1Id == request.PlayerId || m.Player2Id == request.PlayerId)
            && m.Status != MatchStatus.Finished
            && m.Status != MatchStatus.Canceled)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    
        if (match != null)
        {
            return new PollingStatusResponseDto(
                PollingStatus.MatchFound.ToString(),
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
