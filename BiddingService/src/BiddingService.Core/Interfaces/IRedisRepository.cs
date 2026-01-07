using BiddingService.Core.DTOs;
using BiddingService.Core.Entities;

namespace BiddingService.Core.Interfaces;

public interface IRedisRepository
{
    Task<bool> PlaceBidAsync(Bid bid, TimeSpan ttl, bool isExistingBidder = false);
    Task<Bid?> GetHighestBidAsync(long auctionId);
    Task<IEnumerable<Bid>> GetBidHistoryAsync(long auctionId, int page = 1, int pageSize = 50);
    Task<long> GetBidCountAsync(long auctionId);
    Task<bool> HasBidAsync(long auctionId, string bidderIdHash);
    Task<Bid?> GetBidByBidderAsync(long auctionId, string bidderIdHash);
    Task<IEnumerable<Bid>> GetBidsByBidderAsync(string bidderId, int page = 1, int pageSize = 50);
    Task<long> GetBidCountByBidderAsync(string bidderId);
    Task AddToDeadLetterQueueAsync(Bid bid, string errorMessage);
    Task<IEnumerable<DeadLetterMetadata>> GetDeadLetterBidsAsync(int count = 100);
    Task RemoveDeadLetterBidAsync(long bidId);
    Task UpdateDeadLetterRetryAsync(long bidId);
    Task<IEnumerable<string>> GetPendingBidMembersAsync(int count = 100);
    Task<Bid?> GetBidInfoAsync(long bidId);
    Task RemovePendingBidMemberAsync(string member);
    Task RemoveBidInfoAsync(long bidId);
}
