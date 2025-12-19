namespace BiddingService.Core.Interfaces;

public interface IAuctionServiceClient
{
    Task<bool> ValidateAuctionAsync(long auctionId);
    Task<decimal?> GetStartingPriceAsync(long auctionId);
}