using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPlanner.Application.Auth.Commands;
using TripPlanner.Application.Auth.Handlers;
using TripPlanner.Application.Common.DTOs.Auth;
using TripPlanner.Application.Common.Interfaces;

namespace TripPlanner.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    RegisterHandler register,
    LoginHandler login,
    RefreshTokenHandler refresh,
    IUserRepository userRepo,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var result = await register.HandleAsync(new RegisterCommand(req.Email, req.Username, req.Password));
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await login.HandleAsync(new LoginCommand(req.Email, req.Password));
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        var result = await refresh.HandleAsync(new RefreshTokenCommand(req.RefreshToken));
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest req)
    {
        await userRepo.RevokeRefreshTokenAsync(req.RefreshToken);
        await userRepo.SaveChangesAsync();
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await userRepo.GetByIdAsync(currentUser.UserId!.Value);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email, user.Username, user.AvatarUrl));
    }
}

public record RefreshTokenRequest(string RefreshToken);
