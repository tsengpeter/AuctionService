namespace AuctionService.Core.DTOs.Responses;

/// <summary>
/// 商品分類資料傳輸物件
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// 分類 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 分類名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 顯示順序
    /// </summary>
    public int DisplayOrder { get; set; }
}