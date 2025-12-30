using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;

namespace AuctionService.Core.Services;

/// <summary>
/// API 回應代碼服務實作
/// </summary>
public class ResponseCodeService : IResponseCodeService
{
    private readonly IResponseCodeRepository _responseCodeRepository;

    public ResponseCodeService(IResponseCodeRepository responseCodeRepository)
    {
        _responseCodeRepository = responseCodeRepository;
    }

    /// <summary>
    /// 根據代碼和語言取得本地化回應資訊
    /// </summary>
    /// <param name="code">回應代碼</param>
    /// <param name="language">語言代碼 (預設: zh-tw)</param>
    /// <returns>本地化回應資訊</returns>
    public async Task<LocalizedResponseInfo?> GetLocalizedResponseAsync(string code, string language = "zh-tw")
    {
        var responseCode = await _responseCodeRepository.GetByCodeAsync(code);
        if (responseCode == null)
        {
            return null;
        }

        // 根據語言選擇訊息
        var message = language.ToLower() switch
        {
            "zh-tw" or "zh-hant" => responseCode.MessageZhTw,
            "en" or "en-us" => responseCode.MessageEn,
            _ => responseCode.MessageZhTw // 預設使用繁體中文
        };

        return new LocalizedResponseInfo
        {
            Code = responseCode.Code,
            Name = responseCode.Name,
            Message = message,
            Category = responseCode.Category,
            Severity = responseCode.Severity
        };
    }

    /// <summary>
    /// 根據代碼取得本地化訊息
    /// </summary>
    /// <param name="code">回應代碼</param>
    /// <param name="language">語言代碼 (預設: zh-tw)</param>
    /// <returns>本地化訊息</returns>
    public async Task<string?> GetLocalizedMessageAsync(string code, string language = "zh-tw")
    {
        var localizedResponse = await GetLocalizedResponseAsync(code, language);
        return localizedResponse?.Message;
    }

    /// <summary>
    /// 取得所有回應代碼
    /// </summary>
    /// <returns>回應代碼清單</returns>
    public async Task<IEnumerable<ResponseCodeDto>> GetAllAsync()
    {
        var responseCodes = await _responseCodeRepository.GetAllAsync();
        return responseCodes.Select(MapToDto);
    }

    /// <summary>
    /// 根據代碼取得回應代碼
    /// </summary>
    /// <param name="code">代碼</param>
    /// <returns>回應代碼</returns>
    public async Task<ResponseCodeDto?> GetByCodeAsync(string code)
    {
        var responseCode = await _responseCodeRepository.GetByCodeAsync(code);
        return responseCode != null ? MapToDto(responseCode) : null;
    }

    /// <summary>
    /// 根據分類取得回應代碼
    /// </summary>
    /// <param name="category">分類</param>
    /// <returns>回應代碼清單</returns>
    public async Task<IEnumerable<ResponseCodeDto>> GetByCategoryAsync(string category)
    {
        var responseCodes = await _responseCodeRepository.GetByCategoryAsync(category);
        return responseCodes.Select(MapToDto);
    }

    /// <summary>
    /// 檢查代碼是否存在
    /// </summary>
    /// <param name="code">回應代碼</param>
    /// <returns>是否存在</returns>
    public async Task<bool> CodeExistsAsync(string code)
    {
        return await _responseCodeRepository.CodeExistsAsync(code);
    }

    /// <summary>
    /// 將實體映射到 DTO
    /// </summary>
    /// <param name="responseCode">回應代碼實體</param>
    /// <returns>回應代碼 DTO</returns>
    private static ResponseCodeDto MapToDto(ResponseCode responseCode)
    {
        return new ResponseCodeDto
        {
            Code = responseCode.Code,
            Name = responseCode.Name,
            MessageZhTw = responseCode.MessageZhTw,
            MessageEn = responseCode.MessageEn,
            Category = responseCode.Category,
            Severity = responseCode.Severity,
            CreatedAt = responseCode.CreatedAt
        };
    }
}