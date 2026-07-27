using MatchmakingEngine.Application.Application.Commands.Matchmaking;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class DeclineMatchCommandHandler : IRequestHandler<DeclineMatchCommand, bool>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeclineMatchCommandHandler(IMatchRepository matchRepo, IUnitOfWork unitOfWork)
    {
        _matchRepository = matchRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeclineMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: true);
        if (match == null || match.Status != MatchStatus.Pending)
            return false;

        if (match.Player1Id != request.PlayerId && match.Player2Id != request.PlayerId)
            return false;

        match.Status = MatchStatus.Canceled;

        _matchRepository.Update(match);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}