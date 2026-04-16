namespace Auction.Application.Queries.GetWatchlist;

public class WatchlistItemDto
{
    public Guid AuctionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal StartingPrice { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public DateTimeOffset AddedAt { get; init; }
}
