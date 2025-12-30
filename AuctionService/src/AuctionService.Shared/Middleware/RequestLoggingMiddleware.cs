using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AuctionService.Shared.Middleware;

/// <summary>
/// 請求記錄中介軟體
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // 獲取或生成 correlation ID
        var correlationId = GetOrCreateCorrelationId(context);

        // 將 correlation ID 添加到日誌範圍
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            _logger.LogInformation("Request: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation("Response: {StatusCode} - {Elapsed}ms",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        const string correlationIdHeader = "X-Correlation-ID";

        // 嘗試從請求標頭獲取 correlation ID
        if (context.Request.Headers.TryGetValue(correlationIdHeader, out var correlationId))
        {
            return correlationId.ToString();
        }

        // 如果不存在，生成新的 correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();

        // 將 correlation ID 添加到回應標頭
        context.Response.Headers[correlationIdHeader] = newCorrelationId;

        return newCorrelationId;
    }
}