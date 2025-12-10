using AuctionService.Shared.Middleware;
using Microsoft.AspNetCore.Builder;

namespace AuctionService.Shared.Extensions;

/// <summary>
/// 中介軟體擴充方法
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// 使用全域異常處理中介軟體
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }

    /// <summary>
    /// 使用請求記錄中介軟體
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}