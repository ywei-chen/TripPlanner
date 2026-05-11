namespace TripPlanner.Application.Common.DTOs.Attractions;

public record AttractionDto(
    Guid Id,
    string Name,
    string? Description,
    string? Category,
    string? Address,
    string? City,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    string? CoverImage,
    decimal Rating,
    string[] Tags,
    bool IsFavorited);

public record SearchAttractionsRequest(
    string? Q,
    string? Category,
    string? City,
    int Page = 1,
    int PageSize = 20);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
