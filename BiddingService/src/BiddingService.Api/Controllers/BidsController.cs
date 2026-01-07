using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Exceptions;
using BiddingService.Core.Interfaces;
using BiddingService.Core.Validators;
using BiddingService.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BiddingService.Api.Controllers;

[ApiController]
public class BidsController : ControllerBase
{
    private readonly IBiddingService _biddingService;
    private readonly IMemberServiceClient _memberServiceClient;
    private readonly ILogger<BidsController> _logger;

    public BidsController(
        IBiddingService biddingService, 
        IMemberServiceClient memberServiceClient,
        ILogger<BidsController> logger)
    {
        _biddingService = biddingService;
        _memberServiceClient = memberServiceClient;
        _logger = logger;
    }

    [HttpPost("api/bids")]
    [ProducesResponseType(typeof(BidResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> CreateBid([FromBody] CreateBidRequest request)
    {
        try
        {
            var bidderId = await ValidateAndGetBidderIdAsync();
            
            _logger.LogInformation($"Creating bid for auction {request.AuctionId} by bidder {bidderId} with amount {request.Amount}");

            var result = await _biddingService.CreateBidAsync(request, bidderId);

            _logger.LogInformation($"Bid created successfully: {result.BidId} for auction {request.AuctionId}");

            return CreatedAtAction(nameof(CreateBid), new { id = result.BidId }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse 
            { 
                ErrorCode = "UNAUTHORIZED", 
                Message = "Invalid or expired token." 
            });
        }
        catch (AuctionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Auction not found when creating bid: {AuctionId}", ex.AuctionId);
            return NotFound(new ErrorResponse
            {
                ErrorCode = ErrorCodes.AUCTION_NOT_FOUND,
                Message = ex.Message
            });
        }
        catch (BidAmountTooLowException ex)
        {
            _logger.LogWarning(ex, "Bid amount too low: {BidAmount}, current highest: {CurrentHighest}", ex.BidAmount, ex.CurrentHighestBid);
            return BadRequest(new ErrorResponse
            {
                ErrorCode = ErrorCodes.BID_AMOUNT_TOO_LOW,
                Message = ex.Message
            });
        }
    }

    [HttpGet("api/auctions/{auctionId}/bids")]
    [ProducesResponseType(typeof(BidHistoryResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetBidHistory(long auctionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        _logger.LogInformation($"Getting bid history for auction {auctionId}, page {page}, pageSize {pageSize}");

        var result = await _biddingService.GetBidHistoryAsync(auctionId, page, pageSize);

        _logger.LogInformation($"Retrieved {result.Bids.Count()} bids for auction {auctionId}");

        return Ok(result);
    }

    [HttpGet("api/me/bids")]
    [ProducesResponseType(typeof(MyBidsResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> GetMyBids([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var bidderId = await ValidateAndGetBidderIdAsync();

            _logger.LogInformation($"Getting my bids for bidder {bidderId}, page {page}, pageSize {pageSize}");

            var result = await _biddingService.GetMyBidsAsync(bidderId, page, pageSize);

            _logger.LogInformation($"Retrieved {result.Bids.Count()} bids for bidder {bidderId}");

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse
            {
                ErrorCode = "UNAUTHORIZED",
                Message = "Invalid or expired token."
            });
        }
    }

    [HttpGet("api/auctions/{auctionId}/highest-bid")]
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

    [HttpGet("api/auctions/{auctionId}/stats")]
    [ProducesResponseType(typeof(AuctionStatsResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetAuctionStats(long auctionId)
    {
        _logger.LogInformation($"Getting auction stats for auction {auctionId}");

        var result = await _biddingService.GetAuctionStatsAsync(auctionId);

        _logger.LogInformation($"Retrieved stats for auction {auctionId}: {result.TotalBids} bids, highest {result.CurrentHighestBid}");

        return Ok(result);
    }

    [HttpGet("api/bids/{bidId}")]
    [ProducesResponseType(typeof(BidResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetBidById(long bidId)
    {
        _logger.LogInformation($"Getting bid by ID: {bidId}");

        var result = await _biddingService.GetBidByIdAsync(bidId);

        _logger.LogInformation($"Retrieved bid {bidId} for auction {result.AuctionId}");

        return Ok(result);
    }

    private async Task<long> ValidateAndGetBidderIdAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader) || 
            string.IsNullOrEmpty(authHeader) || 
            !authHeader.ToString().StartsWith("Bearer "))
        {
            throw new UnauthorizedAccessException("Missing or invalid Authorization header");
        }

        var token = authHeader.ToString().Substring("Bearer ".Length).Trim();
        var validationResult = await _memberServiceClient.ValidateTokenAsync(token);
        
        if (!validationResult.IsValid)
        {
            throw new UnauthorizedAccessException(validationResult.ErrorMessage ?? "Invalid or expired token.");
        }
        
        if (!validationResult.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Token valid but user ID missing.");
        }
        
        return validationResult.UserId.Value;
    }
}
