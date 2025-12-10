using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Entities;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;

namespace AuctionService.Core.Extensions;

/// <summary>
/// Auction 實體的擴充方法
/// </summary>
public static class AuctionExtensions
{
    /// <summary>
    /// 計算拍賣商品的目前狀態
    /// </summary>
    /// <param name="auction">拍賣商品</param>
    /// <returns>拍賣狀態</returns>
    public static AuctionStatus CalculateStatus(this Auction auction)
    {
        var now = DateTime.UtcNow;

        if (now < auction.StartTime)
        {
            return AuctionStatus.Pending;
        }
        else if (now >= auction.StartTime && now < auction.EndTime)
        {
            return AuctionStatus.Active;
        }
        else
        {
            return AuctionStatus.Ended;
        }
    }

    /// <summary>
    /// 將 CreateAuctionRequest 轉換為 Auction 實體
    /// </summary>
    /// <param name="request">建立拍賣請求</param>
    /// <param name="userId">使用者 ID</param>
    /// <returns>Auction 實體</returns>
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

    /// <summary>
    /// 將 Auction 實體轉換為 AuctionListItemDto
    /// </summary>
    /// <param name="auction">拍賣商品實體</param>
    /// <returns>AuctionListItemDto</returns>
    public static AuctionListItemDto ToListItemDto(this Auction auction)
    {
        return new AuctionListItemDto
        {
            Id = auction.Id,
            Title = auction.Name,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.StartingPrice, // 目前先用起標價，之後需要實作出價邏輯
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            Status = auction.CalculateStatus(),
            Category = auction.Category != null ? new CategoryDto
            {
                Id = auction.Category.Id,
                Name = auction.Category.Name
            } : null,
            Seller = new SellerDto
            {
                Id = auction.UserId,
                Username = $"User_{auction.UserId}" // 暫時的實作
            }
        };
    }

    /// <summary>
    /// 將 Auction 實體轉換為 AuctionDetailDto
    /// </summary>
    /// <param name="auction">拍賣商品實體</param>
    /// <returns>AuctionDetailDto</returns>
    public static AuctionDetailDto ToDetailDto(this Auction auction)
    {
        return new AuctionDetailDto
        {
            Id = auction.Id,
            Title = auction.Name,
            Description = auction.Description,
            ImageUrls = new List<string>(), // 目前沒有圖片功能
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.StartingPrice, // 目前先用起標價
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            Status = auction.CalculateStatus(),
            Category = auction.Category != null ? new CategoryDto
            {
                Id = auction.Category.Id,
                Name = auction.Category.Name
            } : null,
            Seller = new SellerDto
            {
                Id = auction.UserId,
                Username = $"User_{auction.UserId}" // 暫時的實作
            },
            Bids = new List<BidDto>() // 目前沒有出價記錄
        };
    }
}