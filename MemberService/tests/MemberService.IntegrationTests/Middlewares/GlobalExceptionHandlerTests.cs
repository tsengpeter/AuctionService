using MemberService.API.Middlewares;
using MemberService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MemberService.IntegrationTests.Middlewares;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var invoked = false;

        RequestDelegate next = (ctx) =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        Assert.True(invoked);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DomainException_ReturnsBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();
        var exception = new TestDomainException("Test domain error");

        RequestDelegate next = (ctx) => throw exception;

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody);

        Assert.NotNull(problemDetails);
        Assert.Equal((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.Equal("Domain Error", problemDetails.Title);
        Assert.Equal("Test domain error", problemDetails.Detail);
        Assert.Equal("/test", problemDetails.Instance);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new ArgumentException("Invalid argument");

        RequestDelegate next = (ctx) => throw exception;

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new KeyNotFoundException();

        RequestDelegate next = (ctx) => throw exception;

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new UnauthorizedAccessException();

        RequestDelegate next = (ctx) => throw exception;

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_ReturnsConflict()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new InvalidOperationException("Invalid operation");

        RequestDelegate next = (ctx) => throw exception;

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnknownException_ReturnsInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Unknown error");

        RequestDelegate next = (ctx) => throw exception;

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Exception_LogsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Test error");

        RequestDelegate next = (ctx) => throw exception;

        // Act
        await _handler.InvokeAsync(context, next);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unhandled exception occurred")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}