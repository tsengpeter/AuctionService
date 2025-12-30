using AuctionService.Core.Entities;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// API 回應代碼 Repository 介面
/// </summary>
public interface IResponseCodeRepository : IRepository<ResponseCode>
{
    /// <summary>
    /// 根據代碼取得回應代碼
    /// </summary>
    Task<ResponseCode?> GetByCodeAsync(string code);

    /// <summary>
    /// 取得指定分類的回應代碼
    /// </summary>
    Task<IEnumerable<ResponseCode>> GetByCategoryAsync(string category);

    /// <summary>
    /// 檢查代碼是否存在
    /// </summary>
    Task<bool> CodeExistsAsync(string code);
}