namespace AuctionService.Core.Entities;

/// <summary>
/// 商品追蹤實體
/// </summary>
public class Follow
{
    /// <summary>
    /// 追蹤記錄唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 使用者 ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 商品 ID
    /// </summary>
    public Guid AuctionId { get; set; }

    /// <summary>
    /// 追蹤時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // 導航屬性
    /// <summary>
    /// 追蹤的商品
    /// </summary>
    public Auction? Auction { get; set; }
}