using Microsoft.EntityFrameworkCore;
using TripPlanner.Application.Share.Handlers;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence.Repositories;

public class ShareRepository(AppDbContext db) : IShareRepository
{
    public Task<ShareLink?> GetByTokenAsync(string token) =>
        db.ShareLinks.Include(s => s.Trip)
            .ThenInclude(t => t.Items)
            .ThenInclude(i => i.Attraction)
            .FirstOrDefaultAsync(s => s.ShareToken == token);

    public Task<List<ShareLink>> GetByTripIdAsync(Guid tripId) =>
        db.ShareLinks.Where(s => s.TripId == tripId).ToListAsync();

    public Task<ShareLink?> GetByIdAsync(Guid id) =>
        db.ShareLinks.FindAsync(id).AsTask();

    public async Task AddAsync(ShareLink link) => await db.ShareLinks.AddAsync(link);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
