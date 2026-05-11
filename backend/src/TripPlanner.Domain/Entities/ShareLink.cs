using TripPlanner.Domain.Enums;

namespace TripPlanner.Domain.Entities;

public class ShareLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string ShareToken { get; set; } = string.Empty;
    public SharePermission Permission { get; set; } = SharePermission.View;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int ViewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;
    public bool IsValid => IsActive && !IsExpired;

    public Trip Trip { get; set; } = null!;
}
