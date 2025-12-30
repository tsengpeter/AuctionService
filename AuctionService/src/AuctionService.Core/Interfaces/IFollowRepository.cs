using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Entities;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 商品追蹤 Repository 介面
/// </summary>
public interface IFollowRepository : IRepository<Follow>
{
    /// <summary>
    /// 移除追蹤記錄
    /// </summary>
    Task RemoveAsync(string userId, Guid auctionId);

    /// <summary>
    /// 根據使用者 ID 取得追蹤清單 (分頁)
    /// </summary>
    Task<(IEnumerable<Follow> Follows, int TotalCount)> GetByUserIdAsync(string userId, AuctionQueryParameters parameters);

    /// <summary>
    /// 檢查追蹤記錄是否存在
    /// </summary>
    Task<bool> ExistsAsync(string userId, Guid auctionId);

    /// <summary>
    /// 檢查是否正在追蹤自己的商品
    /// </summary>
    Task<bool> IsFollowingOwnAuctionAsync(string userId, Guid auctionId);

    /// <summary>
    /// 計算使用者的追蹤數量
    /// </summary>
    Task<int> CountByUserIdAsync(string userId);
}