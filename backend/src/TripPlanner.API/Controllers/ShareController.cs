using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPlanner.Application.Common.DTOs.Share;
using TripPlanner.Application.Share.Handlers;

namespace TripPlanner.API.Controllers;

[ApiController]
public class ShareController(ShareService shareService) : ControllerBase
{
    [HttpPost("api/share/trips/{tripId:guid}")]
    [Authorize]
    public async Task<IActionResult> CreateShareLink(Guid tripId, [FromBody] CreateShareRequest req)
    {
        var link = await shareService.CreateAsync(tripId, req);
        return StatusCode(201, link);
    }

    [HttpGet("api/share/trips/{tripId:guid}/links")]
    [Authorize]
    public async Task<IActionResult> GetLinks(Guid tripId) =>
        Ok(await shareService.GetByTripAsync(tripId));

    [HttpDelete("api/share/links/{linkId:guid}")]
    [Authorize]
    public async Task<IActionResult> Deactivate(Guid linkId)
    {
        await shareService.DeactivateAsync(linkId);
        return NoContent();
    }

    [HttpGet("api/public/trips/{token}")]
    public async Task<IActionResult> GetSharedTrip(string token) =>
        Ok(await shareService.GetSharedTripAsync(token));
}
