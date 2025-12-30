using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;

namespace AuctionService.Core.Extensions;

/// <summary>
/// 商品追蹤擴充方法
/// </summary>
public static class FollowExtensions
{
    /// <summary>
    /// 將 Follow 實體轉換為 FollowDto
    /// </summary>
    public static FollowDto ToFollowDto(this Follow follow)
    {
        if (follow.Auction == null)
        {
            throw new InvalidOperationException("Follow must include Auction navigation property");
        }

        return new FollowDto
        {
            FollowId = follow.Id,
            UserId = follow.UserId,
            AuctionId = follow.AuctionId,
            AuctionName = follow.Auction.Name,
            AuctionStatus = follow.Auction.CalculateStatus(),
            EndTime = follow.Auction.EndTime,
            CreatedAt = follow.CreatedAt
        };
    }
}