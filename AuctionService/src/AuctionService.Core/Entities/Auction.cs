namespace AuctionService.Core.Entities;

/// <summary>
/// 拍賣商品實體
/// </summary>
public class Auction
{
    /// <summary>
    /// 商品唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 商品名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 起標價
    /// </summary>
    public decimal StartingPrice { get; set; }

    /// <summary>
    /// 分類 ID
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// 拍賣開始時間
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 拍賣結束時間
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 建立者使用者 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // 導航屬性
    /// <summary>
    /// 商品分類
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// 商品追蹤記錄
    /// </summary>
    public ICollection<Follow> Follows { get; set; } = new List<Follow>();
}