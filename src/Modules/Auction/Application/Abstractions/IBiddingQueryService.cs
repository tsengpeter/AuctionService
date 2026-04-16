namespace Auction.Application.Abstractions;

public record BidInfoDto(Guid WinnerId, decimal Amount);

public interface IBiddingQueryService
{
    Task<BidInfoDto?> GetHighestBidAsync(Guid auctionId, CancellationToken ct);
}
