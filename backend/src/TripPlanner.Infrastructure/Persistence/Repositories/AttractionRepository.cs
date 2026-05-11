using Microsoft.EntityFrameworkCore;
using TripPlanner.Application.Attractions.Handlers;
using TripPlanner.Application.Common.DTOs.Attractions;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence.Repositories;

public class AttractionRepository(AppDbContext db) : IAttractionRepository
{
    public async Task<PagedResult<Attraction>> SearchAsync(string? q, string? category, string? city, int page, int pageSize)
    {
        var query = db.Attractions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => EF.Functions.ILike(a.Name, $"%{q}%") ||
                                     EF.Functions.ILike(a.Description ?? "", $"%{q}%"));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(a => a.City == city);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Attraction>(items, total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<Attraction?> GetByIdAsync(Guid id) =>
        db.Attractions.FindAsync(id).AsTask();

    public Task<List<Attraction>> GetFavoritesByUserIdAsync(Guid userId) =>
        db.Favorites.Where(f => f.UserId == userId)
            .Include(f => f.Attraction)
            .Select(f => f.Attraction)
            .ToListAsync();

    public Task<bool> IsFavoritedAsync(Guid userId, Guid attractionId) =>
        db.Favorites.AnyAsync(f => f.UserId == userId && f.AttractionId == attractionId);

    public async Task AddFavoriteAsync(Guid userId, Guid attractionId)
    {
        var already = await IsFavoritedAsync(userId, attractionId);
        if (!already)
            await db.Favorites.AddAsync(new Favorite { UserId = userId, AttractionId = attractionId });
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid attractionId)
    {
        var fav = await db.Favorites.FindAsync(userId, attractionId);
        if (fav is not null) db.Favorites.Remove(fav);
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
