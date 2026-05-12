using TripPlanner.Application.Common.DTOs.Trips;
using TripPlanner.Application.Common.Exceptions;
using TripPlanner.Application.Common.Interfaces;
using TripPlanner.Domain.Entities;
using TripPlanner.Domain.Enums;

namespace TripPlanner.Application.Trips.Handlers;

public interface ITripRepository
{
    Task<List<Trip>> GetByUserIdAsync(Guid userId);
    Task<Trip?> GetByIdWithItemsAsync(Guid id);
    Task AddAsync(Trip trip);
    Task DeleteAsync(Trip trip);
    Task AddItemAsync(TripItem item);
    Task<TripItem?> GetItemByIdAsync(Guid itemId);
    Task DeleteItemAsync(TripItem item);
    Task SaveChangesAsync();
}

public class TripService(ITripRepository repo, ICurrentUserService currentUser)
{
    public async Task<List<TripDto>> GetMyTripsAsync()
    {
        var trips = await repo.GetByUserIdAsync(currentUser.UserId!.Value);
        return trips.Select(MapToDto).ToList();
    }

    public async Task<TripDto> GetByIdAsync(Guid id)
    {
        var trip = await repo.GetByIdWithItemsAsync(id)
            ?? throw new NotFoundException(nameof(Trip), id);

        if (trip.UserId != currentUser.UserId && !trip.IsPublic)
            throw new UnauthorizedException();

        return MapToDto(trip);
    }

    public async Task<TripDto> CreateAsync(CreateTripRequest req)
    {
        var trip = new Trip
        {
            UserId = currentUser.UserId!.Value,
            Title = req.Title,
            Description = req.Description,
            StartDate = req.StartDate,
            EndDate = req.EndDate
        };
        await repo.AddAsync(trip);
        await repo.SaveChangesAsync();
        return MapToDto(trip);
    }

    public async Task<TripDto> UpdateAsync(Guid id, UpdateTripRequest req)
    {
        var trip = await GetOwnedTripAsync(id);
        trip.Title = req.Title;
        trip.Description = req.Description;
        trip.CoverImage = req.CoverImage;
        trip.StartDate = req.StartDate;
        trip.EndDate = req.EndDate;
        trip.IsPublic = req.IsPublic;
        trip.UpdatedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync();
        return MapToDto(trip);
    }

    public async Task DeleteAsync(Guid id)
    {
        var trip = await GetOwnedTripAsync(id);
        await repo.DeleteAsync(trip);
        await repo.SaveChangesAsync();
    }

    public async Task<TripItemDto> AddItemAsync(Guid tripId, AddTripItemRequest req)
    {
        await GetOwnedTripAsync(tripId);
        var item = new TripItem
        {
            TripId = tripId,
            AttractionId = req.AttractionId,
            DayNumber = req.DayNumber,
            SortOrder = req.SortOrder,
            CustomName = req.CustomName,
            Notes = req.Notes,
            StartTime = req.StartTime,
            DurationMins = req.DurationMins
        };
        await repo.AddItemAsync(item);
        await repo.SaveChangesAsync();
        return MapItemToDto(item);
    }

    public async Task DeleteItemAsync(Guid tripId, Guid itemId)
    {
        await GetOwnedTripAsync(tripId);
        var item = await repo.GetItemByIdAsync(itemId)
            ?? throw new NotFoundException(nameof(TripItem), itemId);
        await repo.DeleteItemAsync(item);
        await repo.SaveChangesAsync();
    }

    public async Task ReorderItemsAsync(Guid tripId, ReorderTripItemsRequest req)
    {
        var trip = await GetOwnedTripAsync(tripId);
        foreach (var order in req.Items)
        {
            var item = trip.Items.FirstOrDefault(i => i.Id == order.ItemId);
            if (item is null) continue;
            item.DayNumber = order.DayNumber;
            item.SortOrder = order.SortOrder;
        }
        await repo.SaveChangesAsync();
    }

    private async Task<Trip> GetOwnedTripAsync(Guid id)
    {
        var trip = await repo.GetByIdWithItemsAsync(id)
            ?? throw new NotFoundException(nameof(Trip), id);
        if (trip.UserId != currentUser.UserId)
            throw new UnauthorizedException();
        return trip;
    }

    private static TripDto MapToDto(Trip t) => new(
        t.Id, t.Title, t.Description, t.CoverImage,
        t.StartDate, t.EndDate, t.Status.ToString(), t.IsPublic,
        t.CreatedAt, t.Items.OrderBy(i => i.DayNumber).ThenBy(i => i.SortOrder)
                             .Select(MapItemToDto).ToList());

    private static TripItemDto MapItemToDto(TripItem i) => new(
        i.Id, i.AttractionId,
        i.Attraction?.Name ?? i.CustomName,
        i.Attraction?.CoverImage,
        i.DayNumber, i.SortOrder,
        i.CustomName, i.Notes, i.StartTime, i.DurationMins,
        i.Attraction?.Latitude, i.Attraction?.Longitude);
}
