using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Entities;

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
}