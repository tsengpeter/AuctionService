using BiddingService.Shared.Constants;

namespace BiddingService.Core.Exceptions;

public class AuctionNotFoundException : Exception
{
    public AuctionNotFoundException(long auctionId)
        : base($"Auction with ID {auctionId} not found")
    {
        AuctionId = auctionId;
    }

    public long AuctionId { get; }
}

public class AuctionNotActiveException : Exception
{
    public AuctionNotActiveException(long auctionId)
        : base($"Auction with ID {auctionId} is not active")
    {
        AuctionId = auctionId;
    }

    public long AuctionId { get; }
}

public class BidAmountTooLowException : Exception
{
    public BidAmountTooLowException(decimal currentHighestBid, decimal bidAmount)
        : base($"Bid amount {bidAmount} is too low. Current highest bid is {currentHighestBid}")
    {
        CurrentHighestBid = currentHighestBid;
        BidAmount = bidAmount;
    }

    public decimal CurrentHighestBid { get; }
    public decimal BidAmount { get; }
}

public class DuplicateBidException : Exception
{
    public DuplicateBidException(long auctionId, string bidderId)
        : base($"Bidder {bidderId} has already placed a bid on auction {auctionId}")
    {
        AuctionId = auctionId;
        BidderId = bidderId;
    }

    public long AuctionId { get; }
    public string BidderId { get; }
}

public class BidNotFoundException : Exception
{
    public BidNotFoundException(long bidId)
        : base($"Bid with ID {bidId} not found")
    {
        BidId = bidId;
    }

    public long BidId { get; }
}
