using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using StackExchange.Redis;
using System.IO;
using System.Text.Json;
using System.Globalization;

namespace BiddingService.Infrastructure.Repositories;

public class RedisRepository : IRedisRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _luaScript;

    public RedisRepository(IRedisConnection redisConnection)
    {
        _redis = (IConnectionMultiplexer)redisConnection.Connection;
        var assembly = typeof(RedisRepository).Assembly;
        using var stream = assembly.GetManifestResourceStream("BiddingService.Infrastructure.Redis.Scripts.place-bid.lua");
        using var reader = new StreamReader(stream);
        _luaScript = reader.ReadToEnd();
    }

    public async Task<bool> PlaceBidAsync(Bid bid, TimeSpan ttl, bool isExistingBidder = false)
    {
        var db = _redis.GetDatabase();
        var auctionKey = $"auction:{bid.AuctionId}";
        var highestBidKey = $"highest_bid:{bid.AuctionId}";
        var pendingBidsKey = "pending_bids";
        var bidInfoKey = $"bid_info:{bid.BidId}";
        var biddersKey = $"auction:{bid.AuctionId}:bidders";

        var bidJson = JsonSerializer.Serialize(bid);

        var result = await db.ScriptEvaluateAsync(_luaScript,
            new RedisKey[] { auctionKey, highestBidKey, pendingBidsKey, bidInfoKey, biddersKey },
            new RedisValue[] { 
                bid.BidId, 
                bid.BidderId, 
                bid.Amount.Value.ToString(CultureInfo.InvariantCulture), 
                bid.BidAt.ToString("O"), 
                bid.BidderIdHash,
                (long)ttl.TotalSeconds,
                bid.AuctionId,
                bidJson,
                isExistingBidder ? "true" : "false"
            });

        return (int)result == 1;
    }

    public async Task<Bid?> GetHighestBidAsync(long auctionId)
    {
        var db = _redis.GetDatabase();
        var highestBidKey = $"highest_bid:{auctionId}";

        var hash = await db.HashGetAllAsync(highestBidKey);
        if (hash.Length == 0)
            return null;

        return new Bid(
            long.Parse(hash.First(h => h.Name == "bidId").Value),
            auctionId,
            hash.First(h => h.Name == "bidderId").Value,
            hash.First(h => h.Name == "bidderIdHash").Value,
            new Core.ValueObjects.BidAmount(decimal.Parse(hash.First(h => h.Name == "amount").Value)),
            DateTime.Parse(hash.First(h => h.Name == "bidAt").Value, null, DateTimeStyles.RoundtripKind),
            false
        );
    }

    public async Task<IEnumerable<Bid>> GetBidHistoryAsync(long auctionId, int page = 1, int pageSize = 50)
    {
        var db = _redis.GetDatabase();
        var auctionKey = $"auction:{auctionId}:bids";

        // Check if auction has any bids in Redis
        var bidCount = await db.SortedSetLengthAsync(auctionKey);

        if (bidCount == 0)
        {
            return new List<Bid>();
        }

        // Calculate pagination offset
        var skip = (page - 1) * pageSize;
        
        // Get bids from Redis sorted set (sorted by score descending = bid time descending)
        var entries = await db.SortedSetRangeByScoreWithScoresAsync(
            auctionKey,
            order: Order.Descending,
            skip: skip,
            take: pageSize
        );

        var bids = new List<Bid>();
        foreach (var entry in entries)
        {
            // Member format: "bidId:timestamp:bidderId"
            var parts = entry.Element.ToString().Split(':');
            if (parts.Length >= 3)
            {
                var bidId = long.Parse(parts[0]);
                var timestamp = long.Parse(parts[1]);
                var bidderId = parts[2];
                
                // Get bid details from hash
                var bidHash = await db.HashGetAllAsync($"bid:{bidId}");
                if (bidHash.Length > 0)
                {
                    var amount = decimal.Parse(bidHash.First(h => h.Name == "amount").Value);
                    var bidAt = DateTime.Parse(bidHash.First(h => h.Name == "bidAt").Value, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    var bidderIdHash = bidHash.First(h => h.Name == "bidderIdHash").Value.ToString();
                    
                    bids.Add(new Bid(bidId, auctionId, bidderId, bidderIdHash, new Core.ValueObjects.BidAmount(amount), bidAt, false));
                }
            }
        }

        return bids;
    }

    public async Task<long> GetBidCountAsync(long auctionId)
    {
        var db = _redis.GetDatabase();
        var auctionKey = $"auction:{auctionId}";
        return await db.SortedSetLengthAsync(auctionKey);
    }

    public async Task<bool> HasBidAsync(long auctionId, string bidderIdHash)
    {
        var db = _redis.GetDatabase();
        var biddersKey = $"auction:{auctionId}:bidders";
        return await db.SetContainsAsync(biddersKey, bidderIdHash);
    }

    public async Task<Bid?> GetBidByBidderAsync(long auctionId, string bidderIdHash)
    {
        var db = _redis.GetDatabase();
        
        // Get all bids for this auction from the sorted set
        var bidsKey = $"auction:{auctionId}:bids";
        var allBids = await db.SortedSetRangeByRankAsync(bidsKey, 0, -1, Order.Descending);
        
        // Find the bid from this specific bidder
        foreach (var bidJson in allBids)
        {
            var bid = System.Text.Json.JsonSerializer.Deserialize<Bid>((string)bidJson!);
            if (bid != null && bid.BidderIdHash == bidderIdHash)
            {
                return bid;
            }
        }
        
        return null;
    }

    public async Task<IEnumerable<Bid>> GetBidsByBidderAsync(string bidderId, int page = 1, int pageSize = 50)
    {
        // This is a simplified implementation - in practice, you'd maintain separate indexes
        // For now, we'll return empty list as this requires additional Redis data structures
        return new List<Bid>();
    }

    public async Task<long> GetBidCountByBidderAsync(string bidderId)
    {
        // This is a simplified implementation
        return 0;
    }

    public async Task AddToDeadLetterQueueAsync(Bid bid, string errorMessage)
    {
        var db = _redis.GetDatabase();
        var deadLetterKey = "dead_letter_bids";
        var now = DateTime.UtcNow;
        
        var metadata = new Core.DTOs.DeadLetterMetadata
        {
            Bid = bid,
            RetryCount = 0,
            FirstFailedAt = now,
            LastFailedAt = now,
            ErrorMessage = errorMessage,
            MaxRetries = 3
        };
        
        var metadataJson = JsonSerializer.Serialize(metadata);
        await db.HashSetAsync(deadLetterKey, bid.BidId, metadataJson);
    }

    public async Task<IEnumerable<Core.DTOs.DeadLetterMetadata>> GetDeadLetterBidsAsync(int count = 100)
    {
        var db = _redis.GetDatabase();
        var deadLetterKey = "dead_letter_bids";
        var entries = await db.HashGetAllAsync(deadLetterKey);
        
        var metadataList = new List<Core.DTOs.DeadLetterMetadata>();
        
        foreach (var entry in entries.Take(count))
        {
            var metadata = JsonSerializer.Deserialize<Core.DTOs.DeadLetterMetadata>(entry.Value.ToString());
            if (metadata != null)
            {
                metadataList.Add(metadata);
            }
        }
        
        return metadataList;
    }

    public async Task RemoveDeadLetterBidAsync(long bidId)
    {
        var db = _redis.GetDatabase();
        var deadLetterKey = "dead_letter_bids";
        await db.HashDeleteAsync(deadLetterKey, bidId);
    }
    
    public async Task UpdateDeadLetterRetryAsync(long bidId)
    {
        var db = _redis.GetDatabase();
        var deadLetterKey = "dead_letter_bids";
        var metadataJson = await db.HashGetAsync(deadLetterKey, bidId);
        
        if (!metadataJson.HasValue)
            return;
        
        var metadata = JsonSerializer.Deserialize<Core.DTOs.DeadLetterMetadata>(metadataJson.ToString());
        if (metadata != null)
        {
            metadata.RetryCount++;
            metadata.LastFailedAt = DateTime.UtcNow;
            var updatedJson = JsonSerializer.Serialize(metadata);
            await db.HashSetAsync(deadLetterKey, bidId, updatedJson);
        }
    }

    public async Task<IEnumerable<string>> GetPendingBidMembersAsync(int count = 100)
    {
        var db = _redis.GetDatabase();
        var pendingBidsKey = "pending_bids";
        
        // Use SPOP to pop random members. 
        // Note: This implements at-most-once if we crash after popping.
        // For at-least-once, we should use SRANDMEMBER + SREM, but that requires careful management.
        // Given the prompt discussion, we will use SPOP for simplicity as 'RedisSyncWorker' in plan 
        // implies "read -> write -> remove" but SPOP combines read+remove.
        // Wait, the thought process decided on "Read -> Write -> Remove" pattern using SSCAN/SRANDMEMBER.
        // But SRANDMEMBER might return same items.
        // Let's use SPOP. If worker crashes, data is in 'bid_info' but lost from queue. 
        // We can have a recovery process that scans 'bid_info:*' keys? 
        // The plan says "寫入成功後從 pending_bids Set 移除". This implies Read-then-Remove.
        // So I will use SRANDMEMBER.
        
        var members = await db.SetRandomMembersAsync(pendingBidsKey, count);
        return members.Select(m => m.ToString());
    }

    public async Task<Bid?> GetBidInfoAsync(long bidId)
    {
        var db = _redis.GetDatabase();
        var bidInfoKey = $"bid_info:{bidId}";
        var json = await db.StringGetAsync(bidInfoKey);
        
        if (!json.HasValue) return null;
        
        return JsonSerializer.Deserialize<Bid>(json.ToString());
    }

    public async Task RemovePendingBidMemberAsync(string member)
    {
        var db = _redis.GetDatabase();
        var pendingBidsKey = "pending_bids";
        await db.SetRemoveAsync(pendingBidsKey, member);
    }

    public async Task RemoveBidInfoAsync(long bidId)
    {
        var db = _redis.GetDatabase();
        var bidInfoKey = $"bid_info:{bidId}";
        await db.KeyDeleteAsync(bidInfoKey);
    }
}
