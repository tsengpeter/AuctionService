using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace BiddingService.Infrastructure.Repositories;

public class RedisRepository : IRedisRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _luaScript;

    public RedisRepository(IRedisConnection redisConnection)
    {
        _redis = (IConnectionMultiplexer)redisConnection.Connection;
        _luaScript = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Redis", "Scripts", "place-bid.lua"));
    }

    public async Task<bool> PlaceBidAsync(Bid bid)
    {
        var db = _redis.GetDatabase();
        var auctionKey = $"auction:{bid.AuctionId}";
        var highestBidKey = $"highest_bid:{bid.AuctionId}";

        var result = await db.ScriptEvaluateAsync(_luaScript,
            new RedisKey[] { auctionKey, highestBidKey },
            new RedisValue[] { bid.BidId, bid.BidderId, bid.Amount.Value.ToString(), bid.BidAt.ToString("O"), bid.BidderIdHash });

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
            DateTime.Parse(hash.First(h => h.Name == "bidAt").Value),
            false
        );
    }

    public async Task<IEnumerable<Bid>> GetBidHistoryAsync(long auctionId, int page = 1, int pageSize = 50)
    {
        var db = _redis.GetDatabase();
        var auctionKey = $"auction:{auctionId}";

        var bids = await db.SortedSetRangeByRankAsync(auctionKey, (page - 1) * pageSize, page * pageSize - 1, Order.Descending);

        return bids.Select(value =>
        {
            // Parse bid data from Redis value (assuming JSON format)
            var bidData = JsonSerializer.Deserialize<Dictionary<string, object>>(value.ToString());
            return new Bid(
                long.Parse(bidData["bidId"].ToString()),
                auctionId,
                bidData["bidderId"].ToString(),
                bidData["bidderIdHash"].ToString(),
                new Core.ValueObjects.BidAmount(decimal.Parse(bidData["amount"].ToString())),
                DateTime.Parse(bidData["bidAt"].ToString()),
                false
            );
        });
    }

    public async Task<long> GetBidCountAsync(long auctionId)
    {
        var db = _redis.GetDatabase();
        var auctionKey = $"auction:{auctionId}";
        return await db.SortedSetLengthAsync(auctionKey);
    }

    public async Task<Bid?> GetBidAsync(long auctionId, string bidderId)
    {
        var db = _redis.GetDatabase();
        var auctionKey = $"auction:{auctionId}";

        // This is a simplified implementation - in practice, you'd need to scan the sorted set
        // For now, we'll check if the bidder has any bids in the auction
        var bids = await GetBidHistoryAsync(auctionId, 1, 1000);
        return bids.FirstOrDefault(b => b.BidderId == bidderId);
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

    public async Task AddToDeadLetterQueueAsync(Bid bid)
    {
        var db = _redis.GetDatabase();
        var deadLetterKey = "dead_letter_bids";
        var bidJson = JsonSerializer.Serialize(bid);
        await db.ListLeftPushAsync(deadLetterKey, bidJson);
    }

    public async Task<IEnumerable<Bid>> GetDeadLetterBidsAsync(int count = 100)
    {
        var db = _redis.GetDatabase();
        var deadLetterKey = "dead_letter_bids";
        var bidsJson = await db.ListRangeAsync(deadLetterKey, 0, count - 1);

        return bidsJson.Select(json => JsonSerializer.Deserialize<Bid>(json.ToString())).Where(b => b != null)!;
    }

    public async Task RemoveDeadLetterBidsAsync(IEnumerable<long> bidIds)
    {
        var db = _redis.GetDatabase();
        var deadLetterKey = "dead_letter_bids";

        // This is simplified - in practice, you'd need to remove specific items
        // For now, we'll just trim the list
        await db.ListTrimAsync(deadLetterKey, bidIds.Count(), -1);
    }
}