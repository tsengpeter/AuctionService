using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace MemberService.API.Middlewares;

/// <summary>
/// Middleware for logging HTTP requests with structured logging
/// Records request details, processing duration, and response status
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
        var requestId = context.TraceIdentifier;
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.ToString();

        // Use Serilog context to enrich all logs for this request
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Method", method))
        using (LogContext.PushProperty("Path", path))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "Request started: {Method} {Path}{QueryString}",
                    method,
                    path,
                    queryString);

                await _next(context);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Request completed: {Method} {Path} - StatusCode: {StatusCode}, Duration: {DurationMs}ms",
                    method,
                    path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Request failed: {Method} {Path} - Duration: {DurationMs}ms",
                    method,
                    path,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
