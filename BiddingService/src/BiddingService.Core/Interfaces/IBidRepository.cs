using BiddingService.Core.Entities;

namespace BiddingService.Core.Interfaces;

public interface IBidRepository : IRepository<Bid>
{
    Task<Bid?> GetHighestBidAsync(long auctionId);
    Task<IEnumerable<Bid>> GetBidsByAuctionAsync(long auctionId, int page = 1, int pageSize = 50);
    Task<IEnumerable<Bid>> GetBidsByBidderAsync(string bidderId, int page = 1, int pageSize = 50);
    Task<int> GetBidCountAsync(long auctionId);
}