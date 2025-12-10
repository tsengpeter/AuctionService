using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;

namespace AuctionService.Core.Services;

/// <summary>
/// 商品分類服務實作
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="categoryRepository">商品分類資料庫存取</param>
    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    /// <summary>
    /// 取得所有啟用的商品分類
    /// </summary>
    /// <returns>商品分類清單</returns>
    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        // 只返回啟用的分類，並按顯示順序排序
        return categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(MapToCategoryDto)
            .ToList();
    }

    /// <summary>
    /// 將 Category 實體對應到 CategoryDto
    /// </summary>
    private static CategoryDto MapToCategoryDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder
        };
    }
}