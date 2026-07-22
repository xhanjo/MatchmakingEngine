using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Services;
using MediatR;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class JoinQueueCommandHandler : IRequestHandler<JoinQueueCommand, bool>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IMatchmakingQueue _matchmakingQueue;

    public JoinQueueCommandHandler(IPlayerRepository playerRepository, IMatchmakingQueue matchmakingQueue)
    {
        _playerRepository = playerRepository;
        _matchmakingQueue = matchmakingQueue;
    }

    public async Task<bool> Handle(JoinQueueCommand request, CancellationToken cancellationToken)
    {
        if (await _matchmakingQueue.IsPlayerInQueueAsync(request.PlayerId))
            return true;

        var player = await _playerRepository.GetByIdAsync(request.PlayerId, trackChanges: false);
        if (player == null)
            return false;

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

