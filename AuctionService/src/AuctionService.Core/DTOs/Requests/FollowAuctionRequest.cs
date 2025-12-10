namespace AuctionService.Core.DTOs.Requests;

/// <summary>
/// 追蹤商品請求物件
/// </summary>
public class FollowAuctionRequest
{
    /// <summary>
    /// 商品 ID
    /// </summary>
    public Guid AuctionId { get; set; }
}