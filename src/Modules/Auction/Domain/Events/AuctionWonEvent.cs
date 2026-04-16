using AuctionService.Shared.Events;

namespace Auction.Domain.Events;

public sealed class AuctionWonEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public Guid AuctionId { get; }
    public Guid WinnerId { get; }
    public decimal SoldAmount { get; }
    public Guid SellerId { get; }

    public AuctionWonEvent(Guid auctionId, Guid winnerId, decimal soldAmount, Guid sellerId)
    {
        AuctionId = auctionId;
        WinnerId = winnerId;
        SoldAmount = soldAmount;
        SellerId = sellerId;
    }
}
