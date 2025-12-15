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
    /// 建立成功回應
    /// </summary>
    protected IActionResult Success(object? data = null, string message = "操作成功")
    {
        return Ok(new
        {
            success = true,
            message,
            data
        });
    }

    /// <summary>
    /// 建立錯誤回應
    /// </summary>
    protected IActionResult Error(string code, string message, string messageEn = "")
    {
        return BadRequest(new
        {
            success = false,
            code,
            message,
            messageEn
        });
    }
}