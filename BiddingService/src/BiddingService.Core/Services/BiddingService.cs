using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Entities;
using BiddingService.Core.Exceptions;
using BiddingService.Core.Interfaces;
using BiddingService.Core.ValueObjects;
using BiddingService.Shared.Constants;
using BiddingService.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace BiddingService.Core.Services;

public class BiddingService : IBiddingService
{
    private readonly IBidRepository _bidRepository;
    private readonly IRedisRepository _redisRepository;
    private readonly IAuctionServiceClient _auctionServiceClient;
    private readonly ISnowflakeIdGenerator _idGenerator;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<BiddingService> _logger;

    public BiddingService(
        IBidRepository bidRepository,
        IRedisRepository redisRepository,
        IAuctionServiceClient auctionServiceClient,
        ISnowflakeIdGenerator idGenerator,
        IEncryptionService encryptionService,
        ILogger<BiddingService> logger)
    {
        _bidRepository = bidRepository;
        _redisRepository = redisRepository;
        _auctionServiceClient = auctionServiceClient;
        _idGenerator = idGenerator;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<BidResponse> CreateBidAsync(CreateBidRequest request, string bidderId)
    {
        // Validate auction exists and is active
        var auction = await _auctionServiceClient.GetAuctionAsync(request.AuctionId);
        if (auction == null)
        {
            throw new AuctionNotFoundException(request.AuctionId);
        }

        if (!auction.IsActive)
        {
            throw new AuctionNotActiveException(request.AuctionId);
        }

        // Check if bidder already has a bid on this auction
        var existingBid = await _redisRepository.GetBidAsync(request.AuctionId, bidderId);
        if (existingBid != null)
        {
            throw new DuplicateBidException(request.AuctionId, bidderId);
        }

        // Get current highest bid
        var highestBid = await _redisRepository.GetHighestBidAsync(request.AuctionId);
        if (highestBid != null && request.Amount <= highestBid.Amount.Value)
        {
            throw new BidAmountTooLowException(highestBid.Amount.Value, request.Amount);
        }

        // Generate new bid ID
        var bidId = _idGenerator.GenerateId();

        // Encrypt sensitive data
        var encryptedBidderId = _encryptionService.Encrypt(bidderId);
        var encryptedAmount = _encryptionService.Encrypt(request.Amount.ToString());

        // Create bid entity
        var bid = new Bid(bidId, request.AuctionId, encryptedBidderId, HashHelper.ComputeSha256Hash(bidderId), new BidAmount(request.Amount), DateTime.UtcNow);

        // Store in Redis first (fast path)
        var success = await _redisRepository.PlaceBidAsync(bid);
        if (!success)
        {
            throw new BidAmountTooLowException(highestBid?.Amount.Value ?? 0, request.Amount);
        }

        // Return response (don't wait for background sync)
        return new BidResponse
        {
            BidId = bid.BidId,
            AuctionId = bid.AuctionId,
            Amount = bid.Amount.Value,
            BidAt = bid.BidAt
        };
    }

    public async Task<BidHistoryResponse> GetBidHistoryAsync(long auctionId, int page = 1, int pageSize = 50)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var bids = await _redisRepository.GetBidHistoryAsync(auctionId, page, pageSize);
        var totalCount = (int)await _redisRepository.GetBidCountAsync(auctionId);

        stopwatch.Stop();

        _logger.LogInformation(
            "Retrieved bid history for auction {AuctionId}, page {Page}, pageSize {PageSize}, returned {Count} bids, total {TotalCount}, query time {QueryTime}ms",
            auctionId, page, pageSize, bids.Count(), totalCount, stopwatch.ElapsedMilliseconds);

        return new BidHistoryResponse
        {
            AuctionId = auctionId,
            Bids = bids.Select(b => new BidResponse
            {
                BidId = b.BidId,
                AuctionId = b.AuctionId,
                Amount = b.Amount.Value,
                BidAt = b.BidAt
            }).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
    }

    public async Task<BidHistoryResponse> GetMyBidsAsync(string bidderId, int page = 1, int pageSize = 50)
    {
        var bids = await _redisRepository.GetBidsByBidderAsync(bidderId, page, pageSize);
        var totalCount = (int)await _redisRepository.GetBidCountByBidderAsync(bidderId);

        return new BidHistoryResponse
        {
            Bids = bids.Select(b => new BidResponse
            {
                BidId = b.BidId,
                AuctionId = b.AuctionId,
                Amount = b.Amount.Value,
                BidAt = b.BidAt
            }).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
    }

    public async Task<HighestBidResponse> GetHighestBidAsync(long auctionId)
    {
        var highestBid = await _redisRepository.GetHighestBidAsync(auctionId);

        if (highestBid == null)
        {
            return new HighestBidResponse
            {
                AuctionId = auctionId,
                HighestBid = null
            };
        }

        return new HighestBidResponse
        {
            AuctionId = auctionId,
            HighestBid = new BidResponse
            {
                BidId = highestBid.BidId,
                AuctionId = highestBid.AuctionId,
                Amount = highestBid.Amount.Value,
                BidAt = highestBid.BidAt
            }
        };
    }
}