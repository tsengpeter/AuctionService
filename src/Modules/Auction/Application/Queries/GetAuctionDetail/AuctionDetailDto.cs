using Auction.Domain.Entities;

namespace Auction.Application.Queries.GetAuctionDetail;

public class AuctionDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal StartingPrice { get; init; }
    public decimal? CurrentHighestBid { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public Guid? CategoryId { get; init; }
    public List<AuctionImageDto> Images { get; init; } = [];
}
