using BiddingService.Core.Entities;

namespace BiddingService.Core.Interfaces;

public interface IRedisRepository
{
    Task<bool> PlaceBidAsync(Bid bid);
    Task<Bid?> GetHighestBidAsync(long auctionId);
    Task AddToDeadLetterQueueAsync(Bid bid);
    Task<IEnumerable<Bid>> GetDeadLetterBidsAsync(int count = 100);
    Task RemoveDeadLetterBidsAsync(IEnumerable<long> bidIds);
}