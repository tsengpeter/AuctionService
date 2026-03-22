using AuctionService.Shared;
using FluentValidation;
using System.Text.Json;

namespace AuctionService.Api.Middleware;

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
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for {Path}", context.Request.Path);
            var errors = ex.Errors.Select(e => new FieldError(e.PropertyName, e.ErrorMessage));
            var response = ApiResponseFactory.ValidationFail(errors);
            await WriteJsonResponse(context, 422, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            var response = ApiResponseFactory.Fail("An unexpected error occurred.", 500);
            await WriteJsonResponse(context, 500, response);
        }
    }

    private static async Task WriteJsonResponse(HttpContext context, int statusCode, object response)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
