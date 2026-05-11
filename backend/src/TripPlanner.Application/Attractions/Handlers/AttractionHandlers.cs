using TripPlanner.Application.Common.DTOs.Attractions;
using TripPlanner.Application.Common.Exceptions;
using TripPlanner.Application.Common.Interfaces;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Application.Attractions.Handlers;

public interface IAttractionRepository
{
    Task<PagedResult<Attraction>> SearchAsync(string? q, string? category, string? city, int page, int pageSize);
    Task<Attraction?> GetByIdAsync(Guid id);
    Task<List<Attraction>> GetFavoritesByUserIdAsync(Guid userId);
    Task<bool> IsFavoritedAsync(Guid userId, Guid attractionId);
    Task AddFavoriteAsync(Guid userId, Guid attractionId);
    Task RemoveFavoriteAsync(Guid userId, Guid attractionId);
    Task SaveChangesAsync();
}

public class AttractionService(
    IAttractionRepository repo,
    ICacheService cache,
    ICurrentUserService currentUser)
{
    private static string SearchCacheKey(string? q, string? cat, string? city, int page, int size)
        => $"cache:attractions:{q}:{cat}:{city}:{page}:{size}";

    public async Task<PagedResult<AttractionDto>> SearchAsync(SearchAttractionsRequest req)
    {
        var cacheKey = SearchCacheKey(req.Q, req.Category, req.City, req.Page, req.PageSize);
        var cached = await cache.GetAsync<PagedResult<AttractionDto>>(cacheKey);
        if (cached is not null) return cached;

        var result = await repo.SearchAsync(req.Q, req.Category, req.City, req.Page, req.PageSize);

        var favoritedIds = currentUser.IsAuthenticated
            ? (await repo.GetFavoritesByUserIdAsync(currentUser.UserId!.Value))
                .Select(a => a.Id).ToHashSet()
            : new HashSet<Guid>();

        var dtos = new PagedResult<AttractionDto>(
            result.Items.Select(a => MapToDto(a, favoritedIds.Contains(a.Id))).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);

        await cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));
        return dtos;
    }

    public async Task<AttractionDto> GetByIdAsync(Guid id)
    {
        var attraction = await repo.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Attraction), id);

        bool isFav = currentUser.IsAuthenticated &&
                     await repo.IsFavoritedAsync(currentUser.UserId!.Value, id);
        return MapToDto(attraction, isFav);
    }

    public async Task<List<AttractionDto>> GetFavoritesAsync()
    {
        var attractions = await repo.GetFavoritesByUserIdAsync(currentUser.UserId!.Value);
        return attractions.Select(a => MapToDto(a, true)).ToList();
    }

    public async Task AddFavoriteAsync(Guid attractionId)
    {
        var exists = await repo.GetByIdAsync(attractionId)
            ?? throw new NotFoundException(nameof(Attraction), attractionId);
        await repo.AddFavoriteAsync(currentUser.UserId!.Value, attractionId);
        await repo.SaveChangesAsync();
    }

    public async Task RemoveFavoriteAsync(Guid attractionId)
    {
        await repo.RemoveFavoriteAsync(currentUser.UserId!.Value, attractionId);
        await repo.SaveChangesAsync();
    }

    private static AttractionDto MapToDto(Attraction a, bool isFav) => new(
        a.Id, a.Name, a.Description, a.Category,
        a.Address, a.City, a.Country,
        a.Latitude, a.Longitude, a.CoverImage,
        a.Rating, a.Tags, isFav);
}
