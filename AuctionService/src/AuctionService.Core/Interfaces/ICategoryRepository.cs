using AuctionService.Core.Entities;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 商品分類 Repository 介面
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// 取得啟用的分類
    /// </summary>
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();

    /// <summary>
    /// 根據名稱取得分類
    /// </summary>
    Task<Category?> GetByNameAsync(string name);

    /// <summary>
    /// 檢查分類名稱是否已存在
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}