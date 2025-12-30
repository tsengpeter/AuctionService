namespace AuctionService.Core.DTOs.Requests;

/// <summary>
/// 更新拍賣商品請求 DTO
/// </summary>
public class UpdateAuctionRequest
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
    /// 拍賣結束時間
    /// </summary>
    public DateTime EndTime { get; set; }
}