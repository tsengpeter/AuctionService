using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Entities;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 拍賣商品 Repository 介面
/// </summary>
public interface IAuctionRepository : IRepository<Auction>
{
    /// <summary>
    /// 根據查詢參數取得商品清單 (分頁)
    /// </summary>
    Task<(IEnumerable<Auction> Auctions, int TotalCount)> GetAuctionsAsync(AuctionQueryParameters parameters);

    /// <summary>
    /// 根據 ID 取得商品詳細資訊 (包含關聯資料)
    /// </summary>
    Task<Auction?> GetAuctionByIdAsync(Guid id);

    /// <summary>
    /// 根據使用者 ID 取得商品清單
    /// </summary>
    Task<IEnumerable<Auction>> GetByUserIdAsync(string userId);

    /// <summary>
    /// 根據使用者 ID 取得商品清單 (分頁)
    /// </summary>
    Task<(IEnumerable<Auction> Auctions, int TotalCount)> GetByUserIdAsync(string userId, AuctionQueryParameters parameters);

    /// <summary>
    /// 建立新商品
    /// </summary>
    Task<Auction> CreateAsync(Auction auction);

    /// <summary>
    /// 更新商品
    /// </summary>
    Task<Auction> UpdateAsync(Auction auction);

    /// <summary>
    /// 刪除商品
    /// </summary>
    Task DeleteAsync(Guid id);

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
    Task<bool> IsOwnerAsync(Guid auctionId, string userId);
}