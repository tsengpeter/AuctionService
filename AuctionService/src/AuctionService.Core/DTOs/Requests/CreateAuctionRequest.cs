namespace AuctionService.Core.DTOs.Requests;

/// <summary>
/// 建立拍賣商品請求 DTO
/// </summary>
public class CreateAuctionRequest
{
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
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 拍賣結束時間
    /// </summary>
    public DateTime EndTime { get; set; }
}