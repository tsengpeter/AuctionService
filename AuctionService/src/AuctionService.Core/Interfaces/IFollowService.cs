using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 商品追蹤服務介面
/// </summary>
public interface IFollowService
{
    /// <summary>
    /// 新增追蹤
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>追蹤記錄</returns>
    Task<FollowDto> AddFollowAsync(string userId, Guid auctionId);

    /// <summary>
    /// 移除追蹤
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="auctionId">商品 ID</param>
    Task RemoveFollowAsync(string userId, Guid auctionId);

    /// <summary>
    /// 取得使用者追蹤清單
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="parameters">查詢參數</param>
    /// <returns>分頁追蹤清單</returns>
    Task<PagedResult<FollowDto>> GetUserFollowsAsync(string userId, AuctionQueryParameters parameters);

    /// <summary>
    /// 檢查追蹤是否存在
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>是否存在</returns>
    Task<bool> CheckFollowExistsAsync(string userId, Guid auctionId);
}