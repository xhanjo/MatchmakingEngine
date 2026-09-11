using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Auth;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IPlayerRepository  _playerRepository;
    private readonly IUnitOfWork  _unitOfWork;

    public LogoutCommandHandler(IPlayerRepository playerRepository, IUnitOfWork unitOfWork)
    {
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken)) return;

        var player = await _playerRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (player == null) return;

        var token = player.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);

        if (token != null && token.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}