using AuctionService.Shared;

namespace Ordering.Domain.Entities;

public enum OrderStatus { Pending, Confirmed, Shipped, Completed, Cancelled }

public class Order : BaseEntity
{
    public Guid AuctionId { get; private set; }
    public Guid BuyerId { get; private set; }
    public decimal Amount { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    private Order() { }

    public static Order Create(Guid auctionId, Guid buyerId, decimal amount)
    {
        if (auctionId == Guid.Empty) throw new ArgumentException("AuctionId cannot be empty.", nameof(auctionId));
        if (buyerId == Guid.Empty) throw new ArgumentException("BuyerId cannot be empty.", nameof(buyerId));
        if (amount <= 0) throw new ArgumentException("Amount must be greater than 0.", nameof(amount));

        var now = DateTimeOffset.UtcNow;
        return new Order
        {
            AuctionId = auctionId,
            BuyerId = buyerId,
            Amount = amount,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in status {Status}.");
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel order in status {Status}.");
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
