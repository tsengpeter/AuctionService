using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// API 回應代碼服務介面
/// </summary>
public interface IResponseCodeService
{
    /// <summary>
    /// 根據代碼和語言取得本地化回應資訊
    /// </summary>
    /// <param name="code">回應代碼</param>
    /// <param name="language">語言代碼 (預設: zh-tw)</param>
    /// <returns>本地化回應資訊</returns>
    Task<LocalizedResponseInfo?> GetLocalizedResponseAsync(string code, string language = "zh-tw");

    /// <summary>
    /// 根據代碼取得本地化訊息
    /// </summary>
    /// <param name="code">回應代碼</param>
    /// <param name="language">語言代碼 (預設: zh-tw)</param>
    /// <returns>本地化訊息</returns>
    Task<string?> GetLocalizedMessageAsync(string code, string language = "zh-tw");

    /// <summary>
    /// 取得所有回應代碼
    /// </summary>
    /// <returns>回應代碼清單</returns>
    Task<IEnumerable<ResponseCodeDto>> GetAllAsync();

    /// <summary>
    /// 根據代碼取得回應代碼
    /// </summary>
    /// <param name="code">代碼</param>
    /// <returns>回應代碼</returns>
    Task<ResponseCodeDto?> GetByCodeAsync(string code);

    /// <summary>
    /// 根據分類取得回應代碼
    /// </summary>
    /// <param name="category">分類</param>
    /// <returns>回應代碼清單</returns>
    Task<IEnumerable<ResponseCodeDto>> GetByCategoryAsync(string category);

    /// <summary>
    /// 檢查代碼是否存在
    /// </summary>
    /// <param name="code">回應代碼</param>
    /// <returns>是否存在</returns>
    Task<bool> CodeExistsAsync(string code);
}