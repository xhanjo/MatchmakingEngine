using MatchmakingEngine.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace MatchmakingEngine.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly MatchmakingDbContext _context;

    public AuthController(IConfiguration configuration, MatchmakingDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Username == request.Username);

        if (player == null)
            return Unauthorized("Invalid username or password.");

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash);

        if (!isPasswordValid)
            return Unauthorized("Invalid username or password.");

        var token = GenerateJwtToken(player.Username, player.Id, player.Mmr, player.Role.ToString());

        return Ok(new { Token = token, PlayerId = player.Id });
    }

    private string GenerateJwtToken(string username, Guid playerId, double mmr, string role)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt key is missing");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("PlayerId", playerId.ToString()),
            new Claim("Mmr", mmr.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}

public record LoginRequest(string Username, string Password);
