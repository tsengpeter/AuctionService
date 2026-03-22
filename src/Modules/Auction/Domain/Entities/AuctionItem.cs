using AuctionService.Shared;

namespace Auction.Domain.Entities;

public enum AuctionStatus { Pending, Active, Ended, Sold, Cancelled }

public class AuctionItem : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public decimal StartingPrice { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Pending;
    public DateTimeOffset EndsAt { get; private set; }

    private AuctionItem() { }

    public static AuctionItem Create(string title, decimal startingPrice, DateTimeOffset endsAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (startingPrice <= 0) throw new ArgumentException("StartingPrice must be greater than 0.", nameof(startingPrice));

        var now = DateTimeOffset.UtcNow;
        if (endsAt <= now) throw new ArgumentException("EndsAt must be in the future.", nameof(endsAt));

        return new AuctionItem
        {
            Title = title.Trim(),
            StartingPrice = startingPrice,
            EndsAt = endsAt,
            Status = AuctionStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Activate()
    {
        if (Status != AuctionStatus.Pending)
            throw new InvalidOperationException($"Cannot activate auction in status {Status}.");
        Status = AuctionStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void End()
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException($"Cannot end auction in status {Status}.");
        Status = AuctionStatus.Ended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkSold()
    {
        if (Status != AuctionStatus.Ended)
            throw new InvalidOperationException($"Cannot mark sold from status {Status}.");
        Status = AuctionStatus.Sold;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is AuctionStatus.Sold or AuctionStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel auction in status {Status}.");
        Status = AuctionStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
