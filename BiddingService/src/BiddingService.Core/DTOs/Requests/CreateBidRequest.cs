namespace BiddingService.Core.DTOs.Requests;

public class CreateBidRequest
{
    public long AuctionId { get; set; }
    public decimal Amount { get; set; }
}