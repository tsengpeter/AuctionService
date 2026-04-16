using Auction.Application.Commands.AddWatchlist;
using Auction.Application.Commands.CreateAuction;
using Auction.Application.Commands.RemoveWatchlist;
using Auction.Application.Commands.UpdateAuction;
using Auction.Application.Queries.GetAuctionDetail;
using Auction.Application.Queries.GetAuctions;
using AuctionService.Api.Controllers.Models;
using AuctionService.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuctionService.Api.Controllers;

/// <summary>
/// Manages auction listings and auction details.
/// </summary>
[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuctionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns a paged list of active auctions with optional keyword and category filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuctions(
        [FromQuery] string? q,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAuctionsQuery { Q = q, CategoryId = categoryId, Page = page, PageSize = pageSize }, ct);
        return Ok(ApiResponseFactory.Ok(result));
    }

    /// <summary>
    /// Returns the full detail of a single auction.
    /// </summary>
    [HttpGet("{id:guid}", Name = "GetById")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuctionDetailQuery(id), ct);
        return Ok(ApiResponseFactory.Ok(result));
    }

    /// <summary>
    /// Creates a new auction. OwnerId is derived from the authenticated user's JWT.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateAuction(
        [FromBody] CreateAuctionRequest request,
        CancellationToken ct = default)
    {
        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var id = await _mediator.Send(new CreateAuctionCommand(
            ownerId, request.Title, request.Description, request.StartingPrice,
            request.StartTime, request.EndTime, request.CategoryId, request.ImageUrls), ct);

        return CreatedAtAction(nameof(GetById), new { id }, ApiResponseFactory.Ok(id, 201));
    }

    /// <summary>
    /// Updates an auction. Only the owner can update; sensitive fields forbidden after bidding starts.
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAuction(
        Guid id,
        [FromBody] UpdateAuctionRequest request,
        CancellationToken ct = default)
    {
        var requesterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new UpdateAuctionCommand(
            id, requesterId, request.Title, request.Description, request.StartingPrice,
            request.StartTime, request.EndTime, request.CategoryId, request.ImageUrls), ct);
        return Ok(ApiResponseFactory.Ok(result));
    }

    /// <summary>
    /// Adds an auction to the authenticated user's watchlist.
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/watchlist")]
    public async Task<IActionResult> AddToWatchlist(Guid id, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _mediator.Send(new AddWatchlistCommand(userId, id), ct);
        return NoContent();
    }

    /// <summary>
    /// Removes an auction from the authenticated user's watchlist.
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}/watchlist")]
    public async Task<IActionResult> RemoveFromWatchlist(Guid id, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _mediator.Send(new RemoveWatchlistCommand(userId, id), ct);
        return NoContent();
    }
}
