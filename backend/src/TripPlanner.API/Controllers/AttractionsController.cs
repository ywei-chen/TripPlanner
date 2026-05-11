using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPlanner.Application.Attractions.Handlers;
using TripPlanner.Application.Common.DTOs.Attractions;

namespace TripPlanner.API.Controllers;

[ApiController]
[Route("api/attractions")]
public class AttractionsController(AttractionService attractionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchAttractionsRequest req) =>
        Ok(await attractionService.SearchAsync(req));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) =>
        Ok(await attractionService.GetByIdAsync(id));

    [HttpGet("favorites")]
    [Authorize]
    public async Task<IActionResult> GetFavorites() =>
        Ok(await attractionService.GetFavoritesAsync());

    [HttpPost("{id:guid}/favorite")]
    [Authorize]
    public async Task<IActionResult> AddFavorite(Guid id)
    {
        await attractionService.AddFavoriteAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:guid}/favorite")]
    [Authorize]
    public async Task<IActionResult> RemoveFavorite(Guid id)
    {
        await attractionService.RemoveFavoriteAsync(id);
        return NoContent();
    }
}
