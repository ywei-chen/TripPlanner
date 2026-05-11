using TripPlanner.Application.Auth.Commands;
using TripPlanner.Application.Common.DTOs.Auth;
using TripPlanner.Application.Common.Exceptions;
using TripPlanner.Application.Common.Interfaces;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Application.Auth.Handlers;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task AddAsync(User user);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken token);
    Task RevokeRefreshTokenAsync(string token);
    Task SaveChangesAsync();
}

public class RegisterHandler(
    IUserRepository userRepo,
    IPasswordHasher hasher,
    IJwtService jwt)
{
    public async Task<AuthResponse> HandleAsync(RegisterCommand cmd)
    {
        if (await userRepo.ExistsByEmailAsync(cmd.Email))
            throw new ValidationException(["Email is already taken."]);

        if (await userRepo.ExistsByUsernameAsync(cmd.Username))
            throw new ValidationException(["Username is already taken."]);

        var user = new User
        {
            Email = cmd.Email.ToLowerInvariant(),
            Username = cmd.Username,
            PasswordHash = hasher.Hash(cmd.Password)
        };
        await userRepo.AddAsync(user);

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await userRepo.AddRefreshTokenAsync(refreshToken);
        await userRepo.SaveChangesAsync();

        return AuthHelpers.BuildResponse(user, accessToken, refreshToken.Token);
    }
}

public class LoginHandler(
    IUserRepository userRepo,
    IPasswordHasher hasher,
    IJwtService jwt)
{
    public async Task<AuthResponse> HandleAsync(LoginCommand cmd)
    {
        var user = await userRepo.GetByEmailAsync(cmd.Email.ToLowerInvariant())
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!hasher.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is disabled.");

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await userRepo.AddRefreshTokenAsync(refreshToken);
        await userRepo.SaveChangesAsync();

        return AuthHelpers.BuildResponse(user, accessToken, refreshToken.Token);
    }
}

public class RefreshTokenHandler(
    IUserRepository userRepo,
    IJwtService jwt)
{
    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand cmd)
    {
        var token = await userRepo.GetRefreshTokenAsync(cmd.RefreshToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedException("Refresh token is expired or revoked.");

        await userRepo.RevokeRefreshTokenAsync(cmd.RefreshToken);

        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId,
            Token = jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await userRepo.AddRefreshTokenAsync(newRefreshToken);
        await userRepo.SaveChangesAsync();

        var accessToken = jwt.GenerateAccessToken(token.User);
        return AuthHelpers.BuildResponse(token.User, accessToken, newRefreshToken.Token);
    }
}

file static class AuthHelpers
{
    internal static AuthResponse BuildResponse(User user, string accessToken, string refreshToken) =>
        new(accessToken, refreshToken, 3600,
            new UserDto(user.Id, user.Email, user.Username, user.AvatarUrl));
}
