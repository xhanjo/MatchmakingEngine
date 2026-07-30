using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.Services;
using MediatR;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class JoinQueueCommandHandler : IRequestHandler<JoinQueueCommand, bool>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IMatchmakingQueue _matchmakingQueue;
    private readonly IPartyRepository _partyRepository;

    public JoinQueueCommandHandler(IPlayerRepository playerRepository, IMatchmakingQueue matchmakingQueue, IPartyRepository partyRepository)
    {
        _playerRepository = playerRepository;
        _matchmakingQueue = matchmakingQueue;
        _partyRepository = partyRepository;
    }

    public async Task<bool> Handle(JoinQueueCommand request, CancellationToken cancellationToken)
    {
        if (await _matchmakingQueue.IsPlayerInQueueAsync(request.PlayerId))
            return true;

        var player = await _playerRepository.GetByIdAsync(request.PlayerId, trackChanges: false, cancellationToken);
        if (player == null)
            return false;

        var party = await _partyRepository.GetPartyByPlayerIdAsync(request.PlayerId, trackChanges: false, cancellationToken);

        GameMode mode = GameMode.Solo;
        Guid? partyId = null;
        int searchMmr = player.Mmr;
        double searchTrust = player.TrustFactor;

        if (party != null)
        {
            if (party.LeaderId != request.PlayerId)
                throw new ConflictException("Only the party leader can start matchmaking.");

            if (party.Members.Count != 2)
                throw new ConflictException("You need exactly 2 players in the party to search for a Duo match.");

            mode = party.GameMode;
            partyId = party.Id;

            searchMmr = (int)party.Members.Average(m => m.Player.Mmr);
            searchTrust = party.Members.Average(m => m.Player.TrustFactor);
        }

        var ticket = new MatchmakingTicket
        (
            Guid.NewGuid(),
            player.Id,
            player.Username,
            searchMmr,
            searchTrust,
            player.Region,
            DateTimeOffset.UtcNow,
            mode,
            partyId
        );

        await _matchmakingQueue.EnqueueAsync(ticket);

        return true;
    }
}

