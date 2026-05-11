using TripPlanner.Domain.Entities;

namespace TripPlanner.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
    string? GetJtiFromToken(string token);
    DateTime GetTokenExpiry(string token);
}
