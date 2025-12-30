using AuctionService.Core.Exceptions;
using AuctionService.Shared.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AuctionService.UnitTests.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly GlobalExceptionMiddleware _middleware;

    public GlobalExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _nextMock.Setup(next => next(context)).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode); // Default status
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ReturnsBadRequestWithValidationErrors()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var validationErrors = new List<FluentValidation.Results.ValidationFailure>
        {
            new FluentValidation.Results.ValidationFailure("Name", "Name is required"),
            new FluentValidation.Results.ValidationFailure("Price", "Price must be greater than 0")
        };
        var exception = new ValidationException(validationErrors);
        _nextMock.Setup(next => next(context)).Throws(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.Equal("VALIDATION_ERROR", response.GetProperty("code").GetString());
        Assert.Equal("請求資料驗證失敗", response.GetProperty("message").GetString());
        Assert.True(response.TryGetProperty("errors", out var errors));
        Assert.Equal(2, errors.GetArrayLength());
    }

    [Fact]
    public async Task InvokeAsync_AuctionNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var auctionId = Guid.NewGuid();
        var exception = new AuctionNotFoundException(auctionId);
        _nextMock.Setup(next => next(context)).Throws(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.Equal("AUCTION_NOT_FOUND", response.GetProperty("code").GetString());
        Assert.Equal("找不到商品", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedException_ReturnsForbidden()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var exception = new UnauthorizedException("Access denied");
        _nextMock.Setup(next => next(context)).Throws(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.Equal("UNAUTHORIZED", response.GetProperty("code").GetString());
        Assert.Equal("沒有權限執行此操作", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_GenericException_ReturnsInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var exception = new Exception("Something went wrong");
        _nextMock.Setup(next => next(context)).Throws(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.Equal("INTERNAL_SERVER_ERROR", response.GetProperty("code").GetString());
        Assert.Equal("伺服器發生錯誤，請稍後再試", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_Exception_LogsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Test exception");
        _nextMock.Setup(next => next(context)).Throws(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}