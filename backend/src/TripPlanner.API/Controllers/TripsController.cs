using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPlanner.Application.Common.DTOs.Trips;
using TripPlanner.Application.Trips.Handlers;

namespace TripPlanner.API.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public class TripsController(TripService tripService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyTrips() =>
        Ok(await tripService.GetMyTripsAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTripRequest req)
    {
        var trip = await tripService.CreateAsync(req);
        return StatusCode(201, trip);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) =>
        Ok(await tripService.GetByIdAsync(id));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTripRequest req) =>
        Ok(await tripService.UpdateAsync(id, req));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await tripService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddTripItemRequest req)
    {
        var item = await tripService.AddItemAsync(id, req);
        return StatusCode(201, item);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId)
    {
        await tripService.DeleteItemAsync(id, itemId);
        return NoContent();
    }

    [HttpPut("{id:guid}/items/reorder")]
    public async Task<IActionResult> ReorderItems(Guid id, [FromBody] ReorderTripItemsRequest req)
    {
        await tripService.ReorderItemsAsync(id, req);
        return NoContent();
    }
}
