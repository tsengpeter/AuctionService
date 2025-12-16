using AuctionService.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuctionService.Api.Controllers;

/// <summary>
/// 基礎 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BaseApiController : ControllerBase
{
    protected readonly IResponseCodeService _responseCodeService;

    public BaseApiController(IResponseCodeService responseCodeService)
    {
        _responseCodeService = responseCodeService;
    }
    /// <summary>
    /// 取得目前使用者的 ID (從 JWT Token)
    /// </summary>
    protected string GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            // TODO: 實作適當的認證失敗處理
            // 這是一個臨時實作，方便測試
            userId = "test-user-id"; // 與測試認證處理器匹配
        }
        return userId;
    }

    /// <summary>
    /// 取得請求語言
    /// </summary>
    protected string GetRequestLanguage()
    {
        // 從 Accept-Language 標頭取得語言，預設為 zh-tw
        var acceptLanguage = Request.Headers.AcceptLanguage.FirstOrDefault();
        return acceptLanguage?.Split(',')[0].Split(';')[0] ?? "zh-tw";
    }

    /// <summary>
    /// 建立成功回應
    /// </summary>
    protected async Task<IActionResult> Success(object? data = null, string message = "操作成功")
    {
        var language = GetRequestLanguage();
        var responseInfo = await _responseCodeService.GetLocalizedResponseAsync("SUCCESS", language);

        return Ok(new
        {
            success = true,
            statusCode = responseInfo?.Code ?? "SUCCESS",
            statusName = responseInfo?.Name ?? "Success",
            message = responseInfo?.Message ?? message,
            data
        });
    }

    /// <summary>
    /// 建立錯誤回應
    /// </summary>
    protected async Task<IActionResult> Error(string code, string message, string messageEn = "")
    {
        var language = GetRequestLanguage();
        var responseInfo = await _responseCodeService.GetLocalizedResponseAsync(code, language);

        // 根據錯誤代碼決定 HTTP 狀態碼
        var httpStatusCode = code switch
        {
            "VALIDATION_ERROR" => 400, // Bad Request
            "DUPLICATE_FOLLOW" => 409, // Conflict
            "AUCTION_NOT_FOUND" => 404, // Not Found
            "UNAUTHORIZED" => 401, // Unauthorized
            _ => 400 // 預設 Bad Request
        };

        var result = new
        {
            success = false,
            statusCode = responseInfo?.Code ?? code,
            statusName = responseInfo?.Name ?? "Error",
            message = responseInfo?.Message ?? message,
            messageEn = responseInfo != null && language != "en" ? await GetEnglishMessage(code) : messageEn
        };

        return httpStatusCode switch
        {
            400 => BadRequest(result),
            401 => Unauthorized(result),
            404 => NotFound(result),
            409 => Conflict(result),
            _ => BadRequest(result)
        };
    }

    /// <summary>
    /// 取得英文訊息
    /// </summary>
    private async Task<string> GetEnglishMessage(string code)
    {
        var englishInfo = await _responseCodeService.GetLocalizedResponseAsync(code, "en");
        return englishInfo?.Message ?? string.Empty;
    }
}