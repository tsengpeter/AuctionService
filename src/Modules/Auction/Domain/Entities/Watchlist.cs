using AuctionService.Shared;

namespace Auction.Domain.Entities;

public class Watchlist : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid AuctionId { get; private set; }

    private Watchlist() { }

    public static Watchlist Create(Guid userId, Guid auctionId) =>
        new()
        {
            UserId = userId,
            AuctionId = auctionId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
