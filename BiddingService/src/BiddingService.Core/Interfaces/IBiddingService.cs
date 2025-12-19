using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.DTOs.Responses;

namespace BiddingService.Core.Interfaces;

public interface IBiddingService
{
    Task<BidResponse> CreateBidAsync(CreateBidRequest request, string bidderId);
    Task<BidHistoryResponse> GetBidHistoryAsync(long auctionId, int page = 1, int pageSize = 50);
    Task<MyBidsResponse> GetMyBidsAsync(string bidderId, int page = 1, int pageSize = 50);
    Task<HighestBidResponse> GetHighestBidAsync(long auctionId);
}