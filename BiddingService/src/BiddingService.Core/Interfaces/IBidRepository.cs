using BiddingService.Core.Entities;

namespace BiddingService.Core.Interfaces;

public interface IBidRepository : IRepository<Bid>
{
    Task<Bid?> GetHighestBidAsync(long auctionId);
    Task<IEnumerable<Bid>> GetBidsByAuctionAsync(long auctionId, int page = 1, int pageSize = 50);
    Task<IEnumerable<Bid>> GetBidsByBidderAsync(string bidderId, int page = 1, int pageSize = 50);
    Task<IEnumerable<Bid>> GetBidsByBidderIdHashAsync(string bidderIdHash, int page = 1, int pageSize = 50);
    Task<int> GetBidsCountByBidderIdHashAsync(string bidderIdHash);
    Task<int> GetBidCountAsync(long auctionId);
    Task<AuctionStatsData> GetAuctionStatsAsync(long auctionId);
}

public class AuctionStatsData
{
    public int TotalBids { get; set; }
    public int UniqueBidders { get; set; }
    public decimal? AverageBidAmount { get; set; }
    public DateTime? FirstBidAt { get; set; }
    public DateTime? LastBidAt { get; set; }
    public int BidsInLastHour { get; set; }
    public int BidsInLast24Hours { get; set; }
}