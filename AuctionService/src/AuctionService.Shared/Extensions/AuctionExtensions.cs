using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;

namespace AuctionService.Shared.Extensions;

public static class AuctionExtensions
{
    public static AuctionStatus CalculateStatus(this Auction auction)
    {
        var now = DateTime.UtcNow;

        if (now < auction.StartTime)
        {
            return AuctionStatus.Pending;
        }
        else if (now >= auction.StartTime && now <= auction.EndTime)
        {
            return AuctionStatus.Active;
        }
        else
        {
            return AuctionStatus.Ended;
        }
    }

    public static AuctionListItemDto ToListItemDto(this Auction auction)
    {
        return new AuctionListItemDto
        {
            Id = auction.Id,
            Title = auction.Name,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.StartingPrice, // TODO: Get from BiddingService
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            Status = auction.CalculateStatus(),
            Category = auction.Category != null ? new CategoryDto
            {
                Id = auction.Category.Id,
                Name = auction.Category.Name,
                DisplayOrder = auction.Category.DisplayOrder
            } : null,
            // Seller = TODO: Get from UserService
        };
    }

    public static AuctionDetailDto ToDetailDto(this Auction auction, decimal? currentBid = null)
    {
        return new AuctionDetailDto
        {
            Id = auction.Id,
            Title = auction.Name,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            CurrentPrice = currentBid ?? auction.StartingPrice,
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            Status = auction.CalculateStatus(),
            Category = auction.Category != null ? new CategoryDto
            {
                Id = auction.Category.Id,
                Name = auction.Category.Name,
                DisplayOrder = auction.Category.DisplayOrder
            } : null,
            // Seller = TODO: Get from UserService
            // Bids = TODO: Get from BiddingService
        };
    }

    public static Auction ToEntity(this CreateAuctionRequest request, string userId)
    {
        return new Auction
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            StartingPrice = request.StartingPrice,
            CategoryId = request.CategoryId,
            StartTime = request.StartTime ?? DateTime.UtcNow,
            EndTime = request.EndTime,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}