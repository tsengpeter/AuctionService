using AuctionService.Core.Entities;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 拍賣商品 Repository 介面
/// </summary>
public interface IAuctionRepository : IRepository<Auction>
{
    /// <summary>
    /// 根據使用者 ID 取得商品清單
    /// </summary>
    Task<IEnumerable<Auction>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// 根據分類 ID 取得商品清單
    /// </summary>
    Task<IEnumerable<Auction>> GetByCategoryIdAsync(int categoryId);

    /// <summary>
    /// 取得活躍中的商品 (當前時間在開始和結束時間之間)
    /// </summary>
    Task<IEnumerable<Auction>> GetActiveAuctionsAsync();

    /// <summary>
    /// 取得即將結束的商品
    /// </summary>
    Task<IEnumerable<Auction>> GetEndingSoonAsync(int hours = 24);

    /// <summary>
    /// 搜尋商品
    /// </summary>
    Task<IEnumerable<Auction>> SearchAsync(string searchTerm, int? categoryId = null);

    /// <summary>
    /// 檢查使用者是否有權限操作商品
    /// </summary>
    Task<bool> IsOwnerAsync(Guid auctionId, Guid userId);
}