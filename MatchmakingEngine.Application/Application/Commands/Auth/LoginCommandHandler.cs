using MatchmakingEngine.Application.Interfaces.Auth;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IJwtProvider _jwtProvider;

    public LoginCommandHandler(IPlayerRepository playerRepository, IJwtProvider jwtProvider)
    {
        _playerRepository = playerRepository;
        _jwtProvider = jwtProvider;
    }

    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByUsernameAsync(request.Username, cancellationToken: cancellationToken);

        if (player == null || !BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        return _jwtProvider.GenerateToken(player);
    }
}