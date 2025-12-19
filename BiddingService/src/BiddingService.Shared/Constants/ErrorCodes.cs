namespace BiddingService.Shared.Constants;

public static class ErrorCodes
{
    public const string AuctionNotFound = "AUCTION_NOT_FOUND";
    public const string BidAmountTooLow = "BID_AMOUNT_TOO_LOW";
    public const string AuctionEnded = "AUCTION_ENDED";
    public const string InvalidBidAmount = "INVALID_BID_AMOUNT";
    public const string ConcurrentBidConflict = "CONCURRENT_BID_CONFLICT";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}