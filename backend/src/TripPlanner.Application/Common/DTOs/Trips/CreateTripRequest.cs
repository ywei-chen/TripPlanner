namespace TripPlanner.Application.Common.DTOs.Trips;

public record CreateTripRequest(
    string Title,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record UpdateTripRequest(
    string Title,
    string? Description,
    string? CoverImage,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsPublic);

public record AddTripItemRequest(
    Guid? AttractionId,
    int DayNumber,
    string? CustomName,
    string? Notes,
    TimeOnly? StartTime,
    int? DurationMins);

public record ReorderTripItemsRequest(List<TripItemOrderDto> Items);
public record TripItemOrderDto(Guid ItemId, int DayNumber, int SortOrder);
