using AuctionService.Core.Entities;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 商品追蹤 Repository 介面
/// </summary>
public interface IFollowRepository : IRepository<Follow>
{
    /// <summary>
    /// 根據使用者 ID 取得追蹤清單
    /// </summary>
    Task<IEnumerable<Follow>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// 檢查使用者是否已追蹤特定商品
    /// </summary>
    Task<bool> IsFollowingAsync(Guid userId, Guid auctionId);

    /// <summary>
    /// 取得使用者的追蹤數量
    /// </summary>
    Task<int> GetFollowCountAsync(Guid userId);

    /// <summary>
    /// 取消追蹤
    /// </summary>
    Task UnfollowAsync(Guid userId, Guid auctionId);
}