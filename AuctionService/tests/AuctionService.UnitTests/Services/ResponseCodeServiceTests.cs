using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Services;
using FluentAssertions;
using Moq;

namespace AuctionService.UnitTests.Services;

/// <summary>
/// ResponseCodeService 單元測試
/// </summary>
public class ResponseCodeServiceTests
{
    private readonly Mock<IResponseCodeRepository> _responseCodeRepositoryMock;
    private readonly ResponseCodeService _responseCodeService;

    public ResponseCodeServiceTests()
    {
        _responseCodeRepositoryMock = new Mock<IResponseCodeRepository>();
        _responseCodeService = new ResponseCodeService(_responseCodeRepositoryMock.Object);
    }

    [Fact]
    public async Task GetLocalizedResponseAsync_WithValidCode_ReturnsLocalizedResponse()
    {
        // Arrange
        var code = "SUCCESS";
        var responseCode = new ResponseCode
        {
            Code = code,
            Name = "成功",
            MessageZhTw = "操作成功",
            MessageEn = "Operation successful",
            Category = "General",
            Severity = "Info"
        };

        _responseCodeRepositoryMock
            .Setup(repo => repo.GetByCodeAsync(code))
            .ReturnsAsync(responseCode);

        // Act
        var result = await _responseCodeService.GetLocalizedResponseAsync(code, "zh-tw");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(code);
        result.Message.Should().Be("操作成功");
        result.Category.Should().Be("General");
        result.Severity.Should().Be("Info");
    }

    [Fact]
    public async Task GetLocalizedResponseAsync_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        var code = "INVALID";
        _responseCodeRepositoryMock
            .Setup(repo => repo.GetByCodeAsync(code))
            .ReturnsAsync((ResponseCode?)null);

        // Act
        var result = await _responseCodeService.GetLocalizedResponseAsync(code);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLocalizedMessageAsync_WithValidCode_ReturnsMessage()
    {
        // Arrange
        var code = "ERROR";
        var responseCode = new ResponseCode
        {
            Code = code,
            MessageZhTw = "發生錯誤",
            MessageEn = "An error occurred"
        };

        _responseCodeRepositoryMock
            .Setup(repo => repo.GetByCodeAsync(code))
            .ReturnsAsync(responseCode);

        // Act
        var result = await _responseCodeService.GetLocalizedMessageAsync(code, "en");

        // Assert
        result.Should().Be("An error occurred");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllResponseCodes()
    {
        // Arrange
        var responseCodes = new List<ResponseCode>
        {
            new ResponseCode { Code = "SUCCESS", Name = "成功", MessageZhTw = "操作成功", MessageEn = "Success", Category = "General", Severity = "Info", CreatedAt = DateTime.UtcNow },
            new ResponseCode { Code = "ERROR", Name = "錯誤", MessageZhTw = "發生錯誤", MessageEn = "Error", Category = "Error", Severity = "Error", CreatedAt = DateTime.UtcNow }
        };

        _responseCodeRepositoryMock
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(responseCodes);

        // Act
        var result = await _responseCodeService.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Code.Should().Be("SUCCESS");
        result.Last().Code.Should().Be("ERROR");
    }

    [Fact]
    public async Task GetByCodeAsync_WithValidCode_ReturnsDto()
    {
        // Arrange
        var code = "SUCCESS";
        var responseCode = new ResponseCode
        {
            Code = code,
            Name = "成功",
            MessageZhTw = "操作成功",
            MessageEn = "Success",
            Category = "General",
            Severity = "Info",
            CreatedAt = DateTime.UtcNow
        };

        _responseCodeRepositoryMock
            .Setup(repo => repo.GetByCodeAsync(code))
            .ReturnsAsync(responseCode);

        // Act
        var result = await _responseCodeService.GetByCodeAsync(code);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(code);
        result.Name.Should().Be("成功");
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsFilteredResponseCodes()
    {
        // Arrange
        var category = "Error";
        var responseCodes = new List<ResponseCode>
        {
            new ResponseCode { Code = "ERROR_001", Category = category },
            new ResponseCode { Code = "ERROR_002", Category = category }
        };

        _responseCodeRepositoryMock
            .Setup(repo => repo.GetByCategoryAsync(category))
            .ReturnsAsync(responseCodes);

        // Act
        var result = await _responseCodeService.GetByCategoryAsync(category);

        // Assert
        result.Should().HaveCount(2);
        result.All(rc => rc.Category == category).Should().BeTrue();
    }

    [Fact]
    public async Task CodeExistsAsync_WithExistingCode_ReturnsTrue()
    {
        // Arrange
        var code = "SUCCESS";
        _responseCodeRepositoryMock
            .Setup(repo => repo.CodeExistsAsync(code))
            .ReturnsAsync(true);

        // Act
        var result = await _responseCodeService.CodeExistsAsync(code);

        // Assert
        result.Should().BeTrue();
    }
}