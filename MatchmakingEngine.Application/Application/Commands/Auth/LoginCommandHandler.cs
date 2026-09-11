using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Auth;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork  _unitOfWork;

    public LoginCommandHandler(IPlayerRepository playerRepository, IJwtProvider jwtProvider, IUnitOfWork unitOfWork)
    {
        _playerRepository = playerRepository;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByUsernameAsync(request.Username, trackChanges: true, cancellationToken: cancellationToken);

        if (player == null || !BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var accessToken = _jwtProvider.GenerateToken(player);
        var refreshToken = _jwtProvider.GenerateRefreshToken();
        
        player.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            PlayerId = player.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken, refreshToken);
    }
}