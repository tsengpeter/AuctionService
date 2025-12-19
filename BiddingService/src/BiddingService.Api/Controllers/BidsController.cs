using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Interfaces;
using BiddingService.Core.Validators;
using BiddingService.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BiddingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidsController : ControllerBase
{
    private readonly IBiddingService _biddingService;
    private readonly ILogger<BidsController> _logger;

    public BidsController(IBiddingService biddingService, ILogger<BidsController> logger)
    {
        _biddingService = biddingService;
        _logger = logger;
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

        _logger.LogInformation($"Creating bid for auction {request.AuctionId} by bidder {bidderId} with amount {request.Amount}");

        var result = await _biddingService.CreateBidAsync(request, bidderId);

        _logger.LogInformation($"Bid created successfully: {result.BidId} for auction {request.AuctionId}");

        return CreatedAtAction(nameof(CreateBid), new { id = result.BidId }, result);
    }

    [HttpGet("history/{auctionId}")]
    [ProducesResponseType(typeof(BidHistoryResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetBidHistory(long auctionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        _logger.LogInformation($"Getting bid history for auction {auctionId}, page {page}, pageSize {pageSize}");

        var result = await _biddingService.GetBidHistoryAsync(auctionId, page, pageSize);

        _logger.LogInformation($"Retrieved {result.Bids.Count()} bids for auction {auctionId}");

        return Ok(result);
    }

    [HttpGet("my-bids")]
    [ProducesResponseType(typeof(MyBidsResponse), 200)]
    public async Task<IActionResult> GetMyBids([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var bidderId = GetBidderIdFromToken();

        _logger.LogInformation($"Getting my bids for bidder {bidderId}, page {page}, pageSize {pageSize}");

        var result = await _biddingService.GetMyBidsAsync(bidderId, page, pageSize);

        _logger.LogInformation($"Retrieved {result.Bids.Count()} bids for bidder {bidderId}");

        return Ok(result);
    }

    [HttpGet("highest/{auctionId}")]
    [ProducesResponseType(typeof(HighestBidResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetHighestBid(long auctionId)
    {
        _logger.LogInformation($"Getting highest bid for auction {auctionId}");

        var result = await _biddingService.GetHighestBidAsync(auctionId);

        if (result.HighestBid == null)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = ErrorCodes.BID_NOT_FOUND,
                Message = $"No bids found for auction {auctionId}"
            });
        }

        _logger.LogInformation($"Retrieved highest bid for auction {auctionId}: {result.HighestBid.Amount}");

        return Ok(result);
    }

    [HttpGet("auctions/{auctionId}/stats")]
    [ProducesResponseType(typeof(AuctionStatsResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetAuctionStats(long auctionId)
    {
        _logger.LogInformation($"Getting auction stats for auction {auctionId}");

        var result = await _biddingService.GetAuctionStatsAsync(auctionId);

        _logger.LogInformation($"Retrieved stats for auction {auctionId}: {result.TotalBids} bids, highest {result.CurrentHighestBid}");

        return Ok(result);
    }

    [HttpGet("{bidId}")]
    [ProducesResponseType(typeof(BidResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetBidById(long bidId)
    {
        _logger.LogInformation($"Getting bid by ID: {bidId}");

        var result = await _biddingService.GetBidByIdAsync(bidId);

        _logger.LogInformation($"Retrieved bid {bidId} for auction {result.AuctionId}");

        return Ok(result);
    }

    private string GetBidderIdFromToken()
    {
        // TODO: Implement JWT token parsing to get bidder ID
        // For now, return a placeholder
        return "placeholder-bidder-id";
    }
}
