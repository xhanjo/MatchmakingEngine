using MatchmakingEngine.Application.Interfaces;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class LeaveQueueCommandHandler : IRequestHandler<LeaveQueueCommand, bool>
{
    private readonly IMatchmakingQueue _matchmakingQueue;

    public LeaveQueueCommandHandler(IMatchmakingQueue matchmakingQueue)
    {
        _matchmakingQueue = matchmakingQueue;
    }
    public async Task<bool> Handle(LeaveQueueCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _matchmakingQueue.GetTicketAsync(request.PlayerId);

        if (ticket == null)
            return false;

        await _matchmakingQueue.RemovePlayerAsync(ticket);

        return true;
    }
}
