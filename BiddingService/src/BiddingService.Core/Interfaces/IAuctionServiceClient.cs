namespace BiddingService.Core.Interfaces;

public interface IAuctionServiceClient
{
    Task<AuctionInfo?> GetAuctionAsync(long auctionId);
    Task<bool> ValidateAuctionAsync(long auctionId);
    Task<decimal?> GetStartingPriceAsync(long auctionId);
}

public class AuctionInfo
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
    public decimal StartingPrice { get; set; }
    public DateTime EndTime { get; set; }
}