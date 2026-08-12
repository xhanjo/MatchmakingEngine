using MatchmakingEngine.Application.Configuration;
using MatchmakingEngine.Application.Interfaces.Auth;
using MatchmakingEngine.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MatchmakingEngine.Infrastructure.Auth;

public class JwtProvider : IJwtProvider
{
    private readonly JwtSettings _jwtSettings;

    public JwtProvider(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public string GenerateToken(Player player)
    {
        var claims = new[]
        {
             new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
             new Claim("PlayerId", player.Id.ToString()),
             new Claim(ClaimTypes.Name, player.Username),
             new Claim(ClaimTypes.Role, player.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
