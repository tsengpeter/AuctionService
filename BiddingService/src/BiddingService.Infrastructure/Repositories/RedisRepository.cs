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