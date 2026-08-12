using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Interfaces.Auth;

public interface IJwtProvider
{
    string GenerateToken(Player player);
}
