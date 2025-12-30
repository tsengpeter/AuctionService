using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
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

    /// <summary>
    /// 建立新的拍賣商品
    /// </summary>
    /// <param name="request">建立請求</param>
    /// <param name="userId">賣家使用者 ID</param>
    /// <returns>建立的商品詳細資訊</returns>
    Task<AuctionDetailDto> CreateAuctionAsync(CreateAuctionRequest request, string userId);

    /// <summary>
    /// 更新拍賣商品
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <param name="request">更新請求</param>
    /// <param name="userId">賣家使用者 ID</param>
    /// <returns>更新後的商品詳細資訊</returns>
    Task<AuctionDetailDto> UpdateAuctionAsync(Guid id, UpdateAuctionRequest request, string userId);

    /// <summary>
    /// 刪除拍賣商品
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <param name="userId">賣家使用者 ID</param>
    /// <returns>刪除成功</returns>
    Task DeleteAuctionAsync(Guid id, string userId);

    /// <summary>
    /// 取得使用者的拍賣商品
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="parameters">分頁參數</param>
    /// <returns>分頁結果</returns>
    Task<PagedResult<AuctionListItemDto>> GetUserAuctionsAsync(string userId, AuctionQueryParameters parameters);
}