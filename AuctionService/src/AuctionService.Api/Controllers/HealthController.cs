using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Api.Controllers;

/// <summary>
/// 健康檢查控制器
/// </summary>
[Route("api/health")]
[ApiController]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 健康檢查端點
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "AuctionService"
        });
    }
}