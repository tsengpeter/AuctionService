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

        // Also store in database immediately for integration testing
        // In production, this would be done by background worker
        await _bidRepository.AddAsync(bid);

        // Return response
        return new BidResponse
        {
            BidId = bid.BidId,
            AuctionId = bid.AuctionId,
            BidderIdHash = bid.BidderIdHash,
            Amount = bid.Amount.Value,
            BidAt = bid.BidAt
        };
    }

    public async Task<BidHistoryResponse> GetBidHistoryAsync(long auctionId, int page = 1, int pageSize = 50)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Get bid history from database (as per specification: user bid records are queried from PostgreSQL)
        var bids = await _bidRepository.GetBidsByAuctionAsync(auctionId, page, pageSize);
        var totalCount = await _bidRepository.GetBidCountAsync(auctionId);

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

    public async Task<MyBidsResponse> GetMyBidsAsync(string bidderId, int page = 1, int pageSize = 50)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var bidderIdHash = HashHelper.ComputeSha256Hash(bidderId);
        var bids = await _bidRepository.GetBidsByBidderIdHashAsync(bidderIdHash, page, pageSize);
        var totalCount = await _bidRepository.GetBidsCountByBidderIdHashAsync(bidderIdHash);

        // Get unique auction IDs
        var auctionIds = bids.Select(b => b.AuctionId).Distinct().ToList();

        // Batch fetch auction information
        var auctions = await _auctionServiceClient.GetAuctionsBatchAsync(auctionIds);
        var auctionDict = auctions.ToDictionary(a => a.Id);

        // Get highest bids for each auction to determine if user's bid is highest
        var highestBids = new Dictionary<long, BidAmount?>();
        foreach (var auctionId in auctionIds)
        {
            var highestBid = await _redisRepository.GetHighestBidAsync(auctionId);
            highestBids[auctionId] = highestBid?.Amount;
        }

        var myBids = bids.Select(b =>
        {
            auctionDict.TryGetValue(b.AuctionId, out var auction);
            highestBids.TryGetValue(b.AuctionId, out var highestBidAmount);

            return new MyBidResponse
            {
                BidId = b.BidId,
                AuctionId = b.AuctionId,
                AuctionTitle = auction?.Title ?? "Unknown Auction",
                Amount = b.Amount.Value,
                BidAt = b.BidAt,
                IsHighestBid = highestBidAmount != null && b.Amount.Value >= highestBidAmount.Value,
                IsAuctionActive = auction?.IsActive ?? false
            };
        }).ToList();

        stopwatch.Stop();

        _logger.LogInformation(
            "Retrieved my bids for bidder {BidderIdHash}, page {Page}, pageSize {PageSize}, returned {Count} bids, total {TotalCount}, query time {QueryTime}ms",
            bidderIdHash, page, pageSize, myBids.Count, totalCount, stopwatch.ElapsedMilliseconds);

        return new MyBidsResponse
        {
            Bids = myBids,
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Try Redis first for fast access
        var highestBid = await _redisRepository.GetHighestBidAsync(auctionId);

        stopwatch.Stop();

        if (highestBid != null)
        {
            _logger.LogInformation(
                "Retrieved highest bid for auction {AuctionId} from Redis, bid {BidId}, amount {Amount}, query time {QueryTime}ms",
                auctionId, highestBid.BidId, highestBid.Amount.Value, stopwatch.ElapsedMilliseconds);

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

        // Fallback to database if Redis doesn't have the data
        _logger.LogWarning(
            "Highest bid for auction {AuctionId} not found in Redis, falling back to database",
            auctionId);

        var dbStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var dbHighestBid = await _bidRepository.GetHighestBidAsync(auctionId);
        dbStopwatch.Stop();

        if (dbHighestBid == null)
        {
            _logger.LogInformation(
                "No highest bid found for auction {AuctionId} in database either, total query time {TotalTime}ms",
                auctionId, stopwatch.ElapsedMilliseconds + dbStopwatch.ElapsedMilliseconds);

            return new HighestBidResponse
            {
                AuctionId = auctionId,
                HighestBid = null
            };
        }

        _logger.LogInformation(
            "Retrieved highest bid for auction {AuctionId} from database fallback, bid {BidId}, amount {Amount}, Redis query {RedisTime}ms, DB query {DbTime}ms",
            auctionId, dbHighestBid.BidId, dbHighestBid.Amount.Value, stopwatch.ElapsedMilliseconds, dbStopwatch.ElapsedMilliseconds);

        return new HighestBidResponse
        {
            AuctionId = auctionId,
            HighestBid = new BidResponse
            {
                BidId = dbHighestBid.BidId,
                AuctionId = dbHighestBid.AuctionId,
                Amount = dbHighestBid.Amount.Value,
                BidAt = dbHighestBid.BidAt
            }
        };
    }

    public async Task<AuctionStatsResponse> GetAuctionStatsAsync(long auctionId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Get auction basic info for starting price
        var auction = await _auctionServiceClient.GetAuctionAsync(auctionId);
        if (auction == null)
        {
            _logger.LogWarning("Auction {AuctionId} not found when getting stats", auctionId);
            throw new AuctionNotFoundException(auctionId);
        }

        // Get current highest bid from Redis
        var highestBid = await _redisRepository.GetHighestBidAsync(auctionId);

        // Get stats from database
        var statsData = await _bidRepository.GetAuctionStatsAsync(auctionId);

        // Calculate price growth rate
        decimal? priceGrowthRate = null;
        if (highestBid != null && auction.StartingPrice > 0)
        {
            priceGrowthRate = ((highestBid.Amount.Value - auction.StartingPrice) / auction.StartingPrice) * 100;
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Retrieved auction stats for auction {AuctionId}, total bids {TotalBids}, unique bidders {UniqueBidders}, query time {QueryTime}ms",
            auctionId, statsData.TotalBids, statsData.UniqueBidders, stopwatch.ElapsedMilliseconds);

        return new AuctionStatsResponse
        {
            AuctionId = auctionId,
            TotalBids = statsData.TotalBids,
            UniqueBidders = statsData.UniqueBidders,
            StartingPrice = auction.StartingPrice,
            CurrentHighestBid = highestBid?.Amount.Value,
            AverageBidAmount = statsData.AverageBidAmount,
            PriceGrowthRate = priceGrowthRate,
            FirstBidAt = statsData.FirstBidAt,
            LastBidAt = statsData.LastBidAt,
            BidsInLastHour = statsData.BidsInLastHour,
            BidsInLast24Hours = statsData.BidsInLast24Hours
        };
    }

    public async Task<BidResponse> GetBidByIdAsync(long bidId)
    {
        _logger.LogInformation("Getting bid by ID: {BidId}", bidId);

        var bid = await _bidRepository.GetByIdAsync(bidId);
        if (bid == null)
        {
            _logger.LogWarning("Bid not found: {BidId}", bidId);
            throw new BidNotFoundException(bidId);
        }

        return new BidResponse
        {
            BidId = bid.BidId,
            AuctionId = bid.AuctionId,
            BidderIdHash = bid.BidderIdHash,
            Amount = bid.Amount.Value,
            BidAt = bid.BidAt
        };
    }
}
