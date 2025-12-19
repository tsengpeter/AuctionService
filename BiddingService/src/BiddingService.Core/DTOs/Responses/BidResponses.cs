using BiddingService.Core.ValueObjects;

namespace BiddingService.Core.DTOs.Responses;

public class BidResponse
{
    public long BidId { get; set; }
    public long AuctionId { get; set; }
    public string BidderIdHash { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime BidAt { get; set; }
}

public class HighestBidResponse
{
    public long AuctionId { get; set; }
    public BidResponse? HighestBid { get; set; }
}

public class BidHistoryResponse
{
    public long AuctionId { get; set; }
    public IEnumerable<BidResponse> Bids { get; set; } = new List<BidResponse>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}