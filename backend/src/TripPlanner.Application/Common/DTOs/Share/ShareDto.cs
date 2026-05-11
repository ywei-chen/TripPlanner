namespace TripPlanner.Application.Common.DTOs.Share;

public record ShareLinkDto(
    Guid Id,
    string ShareToken,
    string ShareUrl,
    string Permission,
    DateTime? ExpiresAt,
    bool IsActive,
    int ViewCount,
    DateTime CreatedAt);

public record CreateShareRequest(
    string Permission = "view",
    DateTime? ExpiresAt = null);
