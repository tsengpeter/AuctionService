namespace Auction.Application.Queries.GetAuctions;

public class AuctionListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal StartingPrice { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public string? ThumbnailUrl { get; init; }
}
