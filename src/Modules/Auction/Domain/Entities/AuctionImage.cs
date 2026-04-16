namespace Auction.Domain.Entities;

public class AuctionImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AuctionId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    private AuctionImage() { }

    public static AuctionImage Create(Guid auctionId, string url, int displayOrder) =>
        new() { AuctionId = auctionId, Url = url, DisplayOrder = displayOrder };
}
