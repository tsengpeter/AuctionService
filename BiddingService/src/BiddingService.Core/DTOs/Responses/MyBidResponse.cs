namespace BiddingService.Core.DTOs.Responses;

public class MyBidResponse
{
    public long BidId { get; set; }
    public long AuctionId { get; set; }
    public string AuctionTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime BidAt { get; set; }
    public bool IsHighestBid { get; set; }
    public bool IsAuctionActive { get; set; }
}