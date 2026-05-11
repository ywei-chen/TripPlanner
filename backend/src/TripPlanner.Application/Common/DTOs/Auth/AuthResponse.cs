namespace TripPlanner.Application.Common.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto User);

public record UserDto(Guid Id, string Email, string Username, string? AvatarUrl);
