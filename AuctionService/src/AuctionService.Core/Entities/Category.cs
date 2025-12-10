namespace AuctionService.Core.Entities;

/// <summary>
/// 商品分類實體
/// </summary>
public class Category
{
    /// <summary>
    /// 分類唯一識別碼
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

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // 導航屬性
    /// <summary>
    /// 分類下的商品
    /// </summary>
    public ICollection<Auction> Auctions { get; set; } = new List<Auction>();
}