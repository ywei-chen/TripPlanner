namespace TripPlanner.Application.Common.DTOs.Trips;

public record TripDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverImage,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    bool IsPublic,
    DateTime CreatedAt,
    List<TripItemDto> Items);

public record TripItemDto(
    Guid Id,
    Guid? AttractionId,
    string? AttractionName,
    string? AttractionCoverImage,
    int DayNumber,
    int SortOrder,
    string? CustomName,
    string? Notes,
    TimeOnly? StartTime,
    int? DurationMins,
    decimal? AttractionLatitude,
    decimal? AttractionLongitude);
