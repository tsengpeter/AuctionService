using MemberService.API.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MemberService.IntegrationTests.Middlewares;

public class RequestLoggingMiddlewareTests
{
    private readonly Mock<ILogger<RequestLoggingMiddleware>> _loggerMock;
    private readonly RequestLoggingMiddleware _middleware;

    public RequestLoggingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        _middleware = new RequestLoggingMiddleware(
            (ctx) => Task.CompletedTask,
            _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_LogsRequestAndResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Response.StatusCode = 200;

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Incoming request: GET /api/test")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Outgoing response: 200 for GET /api/test")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithResponseBody_PreservesBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";
        context.Response.StatusCode = 201;

        var responseBody = "Test response";
        context.Response.Body = new MemoryStream();
        await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(responseBody));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var result = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(responseBody, result);
    }

    [Fact]
    public async Task InvokeAsync_LogsElapsedTime()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/slow";
        context.Response.StatusCode = 200;

        var middleware = new RequestLoggingMiddleware(async (ctx) =>
        {
            await Task.Delay(10); // Simulate some processing time
        }, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("in") && v.ToString().Contains("ms")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}