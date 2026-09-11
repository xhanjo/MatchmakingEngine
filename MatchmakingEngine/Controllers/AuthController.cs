using MatchmakingEngine.Application.Application.Commands.Auth;
using MatchmakingEngine.Application.DTO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace MatchmakingEngine.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Username, request.Password);
        var authResult = await _mediator.Send(command);
        
        SetRefreshTokenCookie(authResult.RefreshToken);
        
        return Ok(new LoginResponseDto(authResult.AccessToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refresh-token", out var refreshToken))
        {
            return Unauthorized("Refresh token not found.");
        }
        
        var command = new RefreshCommand(refreshToken);
        var authResult = await _mediator.Send(command);
        
        SetRefreshTokenCookie(authResult.RefreshToken);
        return Ok(new LoginResponseDto(authResult.AccessToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refresh-token", out var refreshToken))
        {
            await _mediator.Send(new LogoutCommand(refreshToken));
        }
        
        Response.Cookies.Delete("refresh-token");

        return Ok();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append("refresh-token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.Now.AddDays(7)
        });
    }
}