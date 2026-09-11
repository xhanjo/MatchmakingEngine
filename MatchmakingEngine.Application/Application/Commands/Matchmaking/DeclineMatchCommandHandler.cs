using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class DeclineMatchCommandHandler : IRequestHandler<DeclineMatchCommand, List<Guid>>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlayerRepository _playerRepository;
    private readonly ICacheService _cacheService;

    public DeclineMatchCommandHandler(IMatchRepository matchRepo, IUnitOfWork unitOfWork, IPlayerRepository playerRepository, ICacheService cacheService)
    {
        _matchRepository = matchRepo;
        _unitOfWork = unitOfWork;
        _playerRepository = playerRepository;
        _cacheService = cacheService;
    }

    public async Task<List<Guid>> Handle(DeclineMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: true);
        if (match == null || match.Status != MatchStatus.Pending)
            return new List<Guid>();

        if (!match.Players.Any(p => p.PlayerId == request.PlayerId))
            return new List<Guid>();

        match.Status = MatchStatus.Canceled;

        var player = await _playerRepository.GetByIdAsync(request.PlayerId, trackChanges: true);
        if (player != null)
        {
            player.TrustFactor = Math.Max(0.0, player.TrustFactor - 0.05);
        }

        await _cacheService.RemoveAsync("all_players", cancellationToken);

        _matchRepository.Update(match);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return match.Players.Select(p => p.PlayerId).ToList();
    }
}