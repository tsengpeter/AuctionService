using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 拍賣商品服務介面
/// </summary>
public interface IAuctionService
{
    /// <summary>
    /// 取得拍賣商品清單
    /// </summary>
    /// <param name="parameters">查詢參數</param>
    /// <returns>分頁結果</returns>
    Task<PagedResult<AuctionListItemDto>> GetAuctionsAsync(AuctionQueryParameters parameters);

    /// <summary>
    /// 根據 ID 取得拍賣商品詳細資訊
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <returns>商品詳細資訊</returns>
    Task<AuctionDetailDto?> GetAuctionByIdAsync(Guid id);

    /// <summary>
    /// 取得商品目前出價資訊
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>目前出價資訊</returns>
    Task<CurrentBidDto?> GetCurrentBidAsync(Guid auctionId);
}