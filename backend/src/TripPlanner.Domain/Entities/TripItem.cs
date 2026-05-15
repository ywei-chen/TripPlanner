namespace TripPlanner.Domain.Entities;

public class TripItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Guid? AttractionId { get; set; }
    public int DayNumber { get; set; } = 1;
    public int SortOrder { get; set; } = 0;
    public string? CustomName { get; set; }
    public string? Notes { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int? DurationMins { get; set; }
    public decimal? CustomLatitude { get; set; }
    public decimal? CustomLongitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Trip Trip { get; set; } = null!;
    public Attraction? Attraction { get; set; }
}
