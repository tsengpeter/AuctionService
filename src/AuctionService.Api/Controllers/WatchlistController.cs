using Auction.Application.Queries.GetWatchlist;
using AuctionService.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuctionService.Api.Controllers;

/// <summary>
/// Manages the authenticated user's watchlist.
/// </summary>
[Authorize]
[ApiController]
[Route("api/watchlist")]
public class WatchlistController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns the authenticated user's watchlist, optionally filtered by auction status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWatchlist(
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await mediator.Send(new GetWatchlistQuery(userId, status), ct);
        return Ok(ApiResponseFactory.Ok(result));
    }
}
