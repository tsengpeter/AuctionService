using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Entities;

namespace BiddingService.Core.Extensions;

public static class BidExtensions
{
    public static BidResponse ToResponse(this Bid bid)
    {
        return new BidResponse
        {
            BidId = bid.BidId,
            AuctionId = bid.AuctionId,
            Amount = bid.Amount,
            BidAt = bid.BidAt,
            BidderIdHash = bid.BidderIdHash
        };
    }

    public static BidResponse ToHistoryResponse(this Bid bid)
    {
        return new BidResponse
        {
            BidId = bid.BidId,
            AuctionId = bid.AuctionId,
            Amount = bid.Amount,
            BidAt = bid.BidAt,
            BidderIdHash = bid.BidderIdHash
        };
    }

    public static MyBidResponse ToMyBidResponse(this Bid bid, string auctionTitle, bool isHighestBid)
    {
        return new MyBidResponse
        {
            BidId = bid.BidId,
            AuctionId = bid.AuctionId,
            AuctionTitle = auctionTitle,
            Amount = bid.Amount,
            BidAt = bid.BidAt,
            IsHighestBid = isHighestBid
        };
    }
}
