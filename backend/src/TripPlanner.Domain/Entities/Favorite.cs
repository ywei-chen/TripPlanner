namespace TripPlanner.Domain.Entities;

public class Favorite
{
    public Guid UserId { get; set; }
    public Guid AttractionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Attraction Attraction { get; set; } = null!;
}
