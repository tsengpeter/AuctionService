namespace AuctionService.Core.DTOs.Responses;

/// <summary>
/// 目前出價資訊 DTO
/// </summary>
public class CurrentBidDto
{
    /// <summary>
    /// 商品 ID
    /// </summary>
    public Guid AuctionId { get; set; }

    /// <summary>
    /// 目前最高出價金額
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// 出價次數
    /// </summary>
    public int BidCount { get; set; }

    /// <summary>
    /// 最後出價時間
    /// </summary>
    public DateTime? LastBidTime { get; set; }

    /// <summary>
    /// 目前最高出價者資訊
    /// </summary>
    public BidderDto? HighestBidder { get; set; }
}