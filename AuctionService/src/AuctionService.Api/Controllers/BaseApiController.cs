using Microsoft.AspNetCore.Mvc;

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
    protected Guid GetCurrentUserId()
    {
        // TODO: 實作從 JWT Token 取得使用者 ID 的邏輯
        // 這是一個臨時實作
        return Guid.NewGuid();
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