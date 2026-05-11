using TripPlanner.Application.Common.DTOs.Share;
using TripPlanner.Application.Common.Exceptions;
using TripPlanner.Application.Common.Interfaces;
using TripPlanner.Domain.Entities;
using TripPlanner.Domain.Enums;

namespace TripPlanner.Application.Share.Handlers;

public interface IShareRepository
{
    Task<ShareLink?> GetByTokenAsync(string token);
    Task<List<ShareLink>> GetByTripIdAsync(Guid tripId);
    Task<ShareLink?> GetByIdAsync(Guid id);
    Task AddAsync(ShareLink link);
    Task SaveChangesAsync();
}

public class ShareService(
    IShareRepository repo,
    ICacheService cache,
    ICurrentUserService currentUser)
{
    private const string BaseUrl = "https://tripplanner.example.com/s";

    public async Task<ShareLinkDto> CreateAsync(Guid tripId, CreateShareRequest req)
    {
        var permission = Enum.TryParse<SharePermission>(req.Permission, true, out var p)
            ? p : SharePermission.View;

        var link = new ShareLink
        {
            TripId = tripId,
            ShareToken = GenerateToken(),
            Permission = permission,
            ExpiresAt = req.ExpiresAt
        };
        await repo.AddAsync(link);
        await repo.SaveChangesAsync();
        return MapToDto(link);
    }

    public async Task<List<ShareLinkDto>> GetByTripAsync(Guid tripId)
    {
        var links = await repo.GetByTripIdAsync(tripId);
        return links.Select(MapToDto).ToList();
    }

    public async Task DeactivateAsync(Guid linkId)
    {
        var link = await repo.GetByIdAsync(linkId)
            ?? throw new NotFoundException(nameof(ShareLink), linkId);
        link.IsActive = false;
        await repo.SaveChangesAsync();
    }

    public async Task<object> GetSharedTripAsync(string token)
    {
        var cacheKey = $"cache:shared_trip:{token}";
        var cached = await cache.GetAsync<object>(cacheKey);
        if (cached is not null)
        {
            await cache.IncrementAsync($"views:{token}");
            return cached;
        }

        var link = await repo.GetByTokenAsync(token)
            ?? throw new NotFoundException("ShareLink", token);

        if (!link.IsValid)
            throw new UnauthorizedException("This share link is no longer active.");

        await cache.IncrementAsync($"views:{token}");
        await cache.SetAsync(cacheKey, link.Trip, TimeSpan.FromMinutes(10));
        return link.Trip;
    }

    private static string GenerateToken()
    {
        var bytes = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static ShareLinkDto MapToDto(ShareLink l) => new(
        l.Id, l.ShareToken, $"{BaseUrl}/{l.ShareToken}",
        l.Permission.ToString(), l.ExpiresAt, l.IsActive, l.ViewCount, l.CreatedAt);
}
