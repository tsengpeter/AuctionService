using AuctionService.Core.Entities;

namespace AuctionService.Shared.Extensions;

/// <summary>
/// Auction 實體擴充方法
/// </summary>
public static class AuctionExtensions
{
    /// <summary>
    /// 計算拍賣商品的狀態
    /// </summary>
    /// <param name="auction">拍賣商品</param>
    /// <returns>狀態字串：Pending/Active/Ended</returns>
    public static string CalculateStatus(this Auction auction)
    {
        var now = DateTime.UtcNow;

        if (now < auction.StartTime)
        {
            return "Pending";
        }
        else if (now >= auction.StartTime && now < auction.EndTime)
        {
            return "Active";
        }
        else
        {
            return "Ended";
        }
    }
}