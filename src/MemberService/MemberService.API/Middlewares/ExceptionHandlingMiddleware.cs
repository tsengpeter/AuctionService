using System.Net;
using System.Text.Json;
using MemberService.Domain.Exceptions;
using Serilog;

namespace MemberService.API.Middlewares;

/// <summary>
/// Middleware for handling domain exceptions and returning standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path.Value ?? "",
            TraceId = context.TraceIdentifier ?? ""
        };

        switch (exception)
        {
            case DomainException domainException:
                context.Response.StatusCode = GetStatusCodeForDomainException(domainException);
                response.Code = domainException.Code;
                response.Message = domainException.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Code = "INTERNAL_SERVER_ERROR";
                response.Message = "An unexpected error occurred";
                break;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsJsonAsync(response, options);
    }

    private static int GetStatusCodeForDomainException(DomainException exception)
    {
        return exception switch
        {
            UserNotFoundException => (int)HttpStatusCode.NotFound,
            EmailAlreadyExistsException => (int)HttpStatusCode.Conflict,
            InvalidCredentialsException => (int)HttpStatusCode.Unauthorized,
            InvalidOldPasswordException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.BadRequest
        };
    }
}

/// <summary>
/// Standard error response format.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the error occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The request path that caused the error
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Trace ID for correlating logs
    /// </summary>
    public string TraceId { get; set; } = string.Empty;
}
