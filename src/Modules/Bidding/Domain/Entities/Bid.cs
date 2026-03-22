using AuctionService.Shared;

namespace Bidding.Domain.Entities;

public class Bid : BaseEntity
{
    public Guid AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }

    private Bid() { }

    public static Bid Place(Guid auctionId, Guid bidderId, decimal amount)
    {
        if (auctionId == Guid.Empty) throw new ArgumentException("AuctionId cannot be empty.", nameof(auctionId));
        if (bidderId == Guid.Empty) throw new ArgumentException("BidderId cannot be empty.", nameof(bidderId));
        if (amount <= 0) throw new ArgumentException("Amount must be greater than 0.", nameof(amount));

        var now = DateTimeOffset.UtcNow;
        return new Bid
        {
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount,
            PlacedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
