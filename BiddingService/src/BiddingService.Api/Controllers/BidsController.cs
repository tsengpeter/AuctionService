using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Interfaces;
using BiddingService.Core.Validators;
using Microsoft.AspNetCore.Mvc;

namespace BiddingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidsController : ControllerBase
{
    private readonly IBiddingService _biddingService;

    public BidsController(IBiddingService biddingService)
    {
        _biddingService = biddingService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BidResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> CreateBid([FromBody] CreateBidRequest request)
    {
        // Get bidder ID from JWT token (will be implemented with authentication)
        var bidderId = GetBidderIdFromToken();

        var result = await _biddingService.CreateBidAsync(request, bidderId);

        return CreatedAtAction(nameof(CreateBid), new { id = result.BidId }, result);
    }

    [HttpGet("history/{auctionId}")]
    [ProducesResponseType(typeof(BidHistoryResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetBidHistory(long auctionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _biddingService.GetBidHistoryAsync(auctionId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("my-bids")]
    [ProducesResponseType(typeof(MyBidsResponse), 200)]
    public async Task<IActionResult> GetMyBids([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var bidderId = GetBidderIdFromToken();
        var result = await _biddingService.GetMyBidsAsync(bidderId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("highest/{auctionId}")]
    [ProducesResponseType(typeof(HighestBidResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetHighestBid(long auctionId)
    {
        var result = await _biddingService.GetHighestBidAsync(auctionId);
        return Ok(result);
    }

    private string GetBidderIdFromToken()
    {
        // TODO: Implement JWT token parsing to get bidder ID
        // For now, return a placeholder
        return "placeholder-bidder-id";
    }
}