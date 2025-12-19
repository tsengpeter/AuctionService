namespace BiddingService.Shared.Constants;

public static class ErrorCodes
{
    public const string AUCTION_NOT_FOUND = "AUCTION_NOT_FOUND";
    public const string AUCTION_NOT_ACTIVE = "AUCTION_NOT_ACTIVE";
    public const string BID_AMOUNT_TOO_LOW = "BID_AMOUNT_TOO_LOW";
    public const string DUPLICATE_BID = "DUPLICATE_BID";
    public const string AUCTION_ENDED = "AUCTION_ENDED";
    public const string INVALID_BID_AMOUNT = "INVALID_BID_AMOUNT";
    public const string CONCURRENT_BID_CONFLICT = "CONCURRENT_BID_CONFLICT";
    public const string INTERNAL_SERVER_ERROR = "INTERNAL_SERVER_ERROR";
}