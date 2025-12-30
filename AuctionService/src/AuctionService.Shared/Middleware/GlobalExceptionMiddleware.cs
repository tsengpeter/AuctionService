using AuctionService.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AuctionService.Shared.Middleware;

/// <summary>
/// 全域異常處理中介軟體
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;
        int statusCode;

        switch (exception)
        {
            case Core.Exceptions.ValidationException validationEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    code = "VALIDATION_ERROR",
                    message = "請求資料驗證失敗",
                    messageEn = "Request validation failed",
                    errors = validationEx.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    }),
                    timestamp = DateTime.UtcNow
                };
                break;
            case AuctionNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                response = new
                {
                    code = "AUCTION_NOT_FOUND",
                    message = "找不到商品",
                    messageEn = "Auction not found",
                    timestamp = DateTime.UtcNow
                };
                break;
            case UnauthorizedException:
                statusCode = (int)HttpStatusCode.Forbidden;
                response = new
                {
                    code = "UNAUTHORIZED",
                    message = "沒有權限執行此操作",
                    messageEn = "Unauthorized to perform this action",
                    timestamp = DateTime.UtcNow
                };
                break;
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                response = new
                {
                    code = "INTERNAL_SERVER_ERROR",
                    message = "伺服器發生錯誤，請稍後再試",
                    messageEn = "Internal server error, please try again later",
                    timestamp = DateTime.UtcNow
                };
                break;
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}