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