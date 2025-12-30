using AuctionService.Core.DTOs.Responses;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 商品分類服務介面
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// 取得所有啟用的商品分類
    /// </summary>
    /// <returns>商品分類清單</returns>
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
}