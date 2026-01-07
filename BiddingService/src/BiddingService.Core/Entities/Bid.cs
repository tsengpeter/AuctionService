using BiddingService.Core.ValueObjects;

namespace BiddingService.Core.Entities;

public class Bid
{
    public long BidId { get; private set; }
    public long AuctionId { get; private set; }
    public string BidderId { get; private set; }
    public string BidderIdHash { get; private set; }
    public BidAmount Amount { get; private set; }
    public DateTime BidAt { get; private set; }
    public bool SyncedFromRedis { get; private set; }

    private Bid() { } // EF Core constructor

    public Bid(long bidId, long auctionId, string bidderId, string bidderIdHash, BidAmount amount, DateTime bidAt, bool syncedFromRedis = false)
    {
        BidId = bidId;
        AuctionId = auctionId;
        BidderId = bidderId ?? throw new ArgumentNullException(nameof(bidderId));
        BidderIdHash = bidderIdHash ?? throw new ArgumentNullException(nameof(bidderIdHash));
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        BidAt = bidAt;
        SyncedFromRedis = syncedFromRedis;
    }

    public static Bid Create(long bidId, long auctionId, string bidderId, string bidderIdHash, BidAmount amount)
    {
        return new Bid(bidId, auctionId, bidderId, bidderIdHash, amount, DateTime.UtcNow);
    }
}
