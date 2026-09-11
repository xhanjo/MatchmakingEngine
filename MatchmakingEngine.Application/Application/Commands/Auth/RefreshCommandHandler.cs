using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Auth;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Auth;

public class RefreshCommandHandler : IRequestHandler<RefreshCommand, AuthResult>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshCommandHandler(IPlayerRepository playerRepository, IJwtProvider jwtProvider, IUnitOfWork unitOfWork)
    {
        _playerRepository = playerRepository;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<AuthResult> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (player == null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var existingToken = player.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);

        if (existingToken == null || !existingToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        existingToken.RevokedAt = DateTime.UtcNow;

        var newAccessToken = _jwtProvider.GenerateToken(player);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();
        
        player.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            PlayerId = player.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(newAccessToken, newRefreshToken);
    }
}