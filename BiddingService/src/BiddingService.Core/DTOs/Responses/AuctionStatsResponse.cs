using BiddingService.Core.DTOs.Responses;

namespace BiddingService.Core.DTOs.Responses;

public class AuctionStatsResponse
{
    public long AuctionId { get; set; }
    public int TotalBids { get; set; }
    public int UniqueBidders { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal? CurrentHighestBid { get; set; }
    public decimal? AverageBidAmount { get; set; }
    public decimal? PriceGrowthRate { get; set; } // (CurrentHighest - StartingPrice) / StartingPrice * 100
    public DateTime? FirstBidAt { get; set; }
    public DateTime? LastBidAt { get; set; }
    public int BidsInLastHour { get; set; }
    public int BidsInLast24Hours { get; set; }
}