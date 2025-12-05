using MemberService.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace MemberService.API.Middlewares;

/// <summary>
/// Global exception handler middleware
/// </summary>
public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var (statusCode, title, detail) = GetErrorDetails(exception);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode statusCode, string title, string detail) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            DomainException domainEx => (HttpStatusCode.BadRequest, "Domain Error", domainEx.Message),
            ArgumentException argEx => (HttpStatusCode.BadRequest, "Invalid Argument", argEx.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found", "The requested resource was not found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", "Access is denied"),
            InvalidOperationException invalidOpEx => (HttpStatusCode.Conflict, "Invalid Operation", invalidOpEx.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred")
        };
    }
}