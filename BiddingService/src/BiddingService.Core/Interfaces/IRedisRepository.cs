using BiddingService.Core.Entities;

namespace BiddingService.Core.Interfaces;

public interface IRedisRepository
{
    Task<bool> PlaceBidAsync(Bid bid, TimeSpan ttl);
    Task<Bid?> GetHighestBidAsync(long auctionId);
    Task<IEnumerable<Bid>> GetBidHistoryAsync(long auctionId, int page = 1, int pageSize = 50);
    Task<long> GetBidCountAsync(long auctionId);
    Task<Bid?> GetBidAsync(long auctionId, string bidderId);
    Task<IEnumerable<Bid>> GetBidsByBidderAsync(string bidderId, int page = 1, int pageSize = 50);
    Task<long> GetBidCountByBidderAsync(string bidderId);
    Task AddToDeadLetterQueueAsync(Bid bid);
    Task<IEnumerable<Bid>> GetDeadLetterBidsAsync(int count = 100);
    Task RemoveDeadLetterBidsAsync(IEnumerable<long> bidIds);
    Task<IEnumerable<string>> GetPendingBidMembersAsync(int count = 100);
    Task<Bid?> GetBidInfoAsync(long bidId);
    Task RemovePendingBidMemberAsync(string member);
    Task RemoveBidInfoAsync(long bidId);
}
