using AuctionService.Core.DTOs.Responses;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 出價服務客戶端介面
/// </summary>
public interface IBiddingServiceClient
{
    /// <summary>
    /// 取得商品的目前出價資訊
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>目前出價資訊</returns>
    Task<CurrentBidDto?> GetCurrentBidAsync(Guid auctionId);

    /// <summary>
    /// 檢查商品是否已有出價
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>是否已有出價</returns>
    Task<bool> CheckAuctionHasBidsAsync(Guid auctionId);
}