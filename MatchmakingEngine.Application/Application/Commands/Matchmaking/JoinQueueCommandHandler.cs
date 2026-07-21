using MediatR;
using MatchmakingEngine.Application.Configuration;
using MatchmakingEngine.Services;
using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Application.Interfaces;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class JoinQueueCommandHandler : IRequestHandler<JoinQueueCommand, bool>
{
    private readonly IMatchmakingDbContext _context;
    private readonly IMatchmakingQueue _matchmakingQueue;

    public JoinQueueCommandHandler(IMatchmakingDbContext context, IMatchmakingQueue matchmakingQueue)
    {
        _context = context;
        _matchmakingQueue = matchmakingQueue;
    }
    
    public async Task<bool> Handle(JoinQueueCommand request, CancellationToken cancellationToken)
    {
        if (await _matchmakingQueue.IsPlayerInQueueAsync(request.PlayerId)) return true;

        var player = await _context.Players.FindAsync(new object[] { request.PlayerId }, cancellationToken);
        if (player == null) return false;

        var ticket = new MatchmakingTicket
        (
            Guid.NewGuid(),
            player.Id,
            player.Username,
            player.Mmr,
            player.TrustFactor,
            player.Region,
            DateTimeOffset.UtcNow
        );

        await _matchmakingQueue.EnqueueAsync(ticket);

        return true;
    }
}

