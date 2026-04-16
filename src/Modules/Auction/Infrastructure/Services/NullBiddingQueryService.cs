using Auction.Application.Abstractions;

namespace Auction.Infrastructure.Services;

public class NullBiddingQueryService : IBiddingQueryService
{
    public Task<BidInfoDto?> GetHighestBidAsync(Guid auctionId, CancellationToken ct)
        => Task.FromResult<BidInfoDto?>(null);
}
