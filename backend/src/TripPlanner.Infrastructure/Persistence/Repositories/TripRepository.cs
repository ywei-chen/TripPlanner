using Microsoft.EntityFrameworkCore;
using TripPlanner.Application.Trips.Handlers;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence.Repositories;

public class TripRepository(AppDbContext db) : ITripRepository
{
    public Task<List<Trip>> GetByUserIdAsync(Guid userId) =>
        db.Trips.Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public Task<Trip?> GetByIdWithItemsAsync(Guid id) =>
        db.Trips.Include(t => t.Items)
            .ThenInclude(i => i.Attraction)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(Trip trip) => await db.Trips.AddAsync(trip);

    public Task DeleteAsync(Trip trip)
    {
        db.Trips.Remove(trip);
        return Task.CompletedTask;
    }

    public async Task AddItemAsync(TripItem item) => await db.TripItems.AddAsync(item);

    public Task<TripItem?> GetItemByIdAsync(Guid itemId) =>
        db.TripItems.FindAsync(itemId).AsTask();

    public Task DeleteItemAsync(TripItem item)
    {
        db.TripItems.Remove(item);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
