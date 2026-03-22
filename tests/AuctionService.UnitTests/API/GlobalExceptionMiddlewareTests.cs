using AuctionService.Api.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace AuctionService.UnitTests.API;

public class GlobalExceptionMiddlewareTests
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger =
        Substitute.For<ILogger<GlobalExceptionMiddleware>>();

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldPassThrough()
    {
        var middleware = new GlobalExceptionMiddleware(
            next: _ => Task.CompletedTask,
            logger: _logger);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ShouldReturn422()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Username", "Username is too short")
        };

        var middleware = new GlobalExceptionMiddleware(
            next: _ => throw new ValidationException(failures),
            logger: _logger);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(422);
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ShouldContainFieldErrors()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required")
        };

        var middleware = new GlobalExceptionMiddleware(
            next: _ => throw new ValidationException(failures),
            logger: _logger);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBody(context);
        var doc = JsonDocument.Parse(body).RootElement;

        doc.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.GetProperty("statusCode").GetInt32().Should().Be(422);
        doc.GetProperty("errors").GetArrayLength().Should().Be(1);
        doc.GetProperty("errors")[0].GetProperty("field").GetString().Should().Be("Email");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ShouldReturn500()
    {
        var middleware = new GlobalExceptionMiddleware(
            next: _ => throw new InvalidOperationException("Something went wrong"),
            logger: _logger);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ShouldReturnErrorApiResponse()
    {
        var middleware = new GlobalExceptionMiddleware(
            next: _ => throw new InvalidOperationException("Something went wrong"),
            logger: _logger);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBody(context);
        var doc = JsonDocument.Parse(body).RootElement;

        doc.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.GetProperty("statusCode").GetInt32().Should().Be(500);
    }
}
