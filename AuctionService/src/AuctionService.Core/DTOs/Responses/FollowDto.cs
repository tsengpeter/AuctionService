namespace AuctionService.Core.DTOs.Responses;

using AuctionService.Core.DTOs.Common;

/// <summary>
/// 商品追蹤資料傳輸物件
/// </summary>
public class FollowDto
{
    /// <summary>
    /// 追蹤記錄 ID
    /// </summary>
    public Guid FollowId { get; set; }

    /// <summary>
    /// 使用者 ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 商品 ID
    /// </summary>
    public Guid AuctionId { get; set; }

    /// <summary>
    /// 商品名稱
    /// </summary>
    public string AuctionName { get; set; } = string.Empty;

    /// <summary>
    /// 商品狀態
    /// </summary>
    public AuctionStatus AuctionStatus { get; set; }

    /// <summary>
    /// 目前出價
    /// </summary>
    public decimal? CurrentBid { get; set; }

    /// <summary>
    /// 商品結束時間
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 追蹤建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }
}