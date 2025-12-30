using AuctionService.Core.Entities;
using AuctionService.Infrastructure.Data;
using AuctionService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuctionService.UnitTests.Repositories;

/// <summary>
/// ResponseCodeRepository 單元測試
/// </summary>
public class ResponseCodeRepositoryTests : IDisposable
{
    private readonly AuctionDbContext _context;
    private readonly ResponseCodeRepository _repository;
    private readonly Mock<ILogger<ResponseCodeRepository>> _loggerMock;

    public ResponseCodeRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<ResponseCodeRepository>>();

        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuctionDbContext(options);
        _repository = new ResponseCodeRepository(_context, _loggerMock.Object);

        // 準備測試資料
        SeedTestDataAsync().Wait();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task SeedTestDataAsync()
    {
        var responseCodes = new List<ResponseCode>
        {
            new ResponseCode
            {
                Code = "SUCCESS",
                Name = "成功",
                MessageZhTw = "操作成功",
                MessageEn = "Operation successful",
                Category = "General",
                Severity = "Info",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "ERROR",
                Name = "錯誤",
                MessageZhTw = "發生錯誤",
                MessageEn = "An error occurred",
                Category = "Error",
                Severity = "Error",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "VALIDATION_ERROR",
                Name = "驗證錯誤",
                MessageZhTw = "輸入資料無效",
                MessageEn = "Invalid input data",
                Category = "Validation",
                Severity = "Warning",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.ResponseCodes.AddRangeAsync(responseCodes);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByCodeAsync_WithExistingCode_ReturnsResponseCode()
    {
        // Act
        var result = await _repository.GetByCodeAsync("SUCCESS");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("SUCCESS");
        result.Name.Should().Be("成功");
        result.MessageZhTw.Should().Be("操作成功");
        result.Category.Should().Be("General");
    }

    [Fact]
    public async Task GetByCodeAsync_WithNonExistingCode_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByCodeAsync("NON_EXISTING");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCategoryAsync_WithExistingCategory_ReturnsResponseCodes()
    {
        // Act
        var result = await _repository.GetByCategoryAsync("Error");

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("ERROR");
        result.First().Category.Should().Be("Error");
    }

    [Fact]
    public async Task GetByCategoryAsync_WithNonExistingCategory_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByCategoryAsync("NonExisting");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CodeExistsAsync_WithExistingCode_ReturnsTrue()
    {
        // Act
        var result = await _repository.CodeExistsAsync("SUCCESS");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CodeExistsAsync_WithNonExistingCode_ReturnsFalse()
    {
        // Act
        var result = await _repository.CodeExistsAsync("NON_EXISTING");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllResponseCodes()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(rc => rc.Code).Should().Contain(new[] { "SUCCESS", "ERROR", "VALIDATION_ERROR" });
    }
}