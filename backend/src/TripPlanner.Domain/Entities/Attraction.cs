namespace TripPlanner.Domain.Entities;

public class Attraction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? CoverImage { get; set; }
    public decimal Rating { get; set; } = 0;
    public string[] Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TripItem> TripItems { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
