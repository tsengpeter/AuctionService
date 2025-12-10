using AuctionService.Core.Entities;
using AuctionService.Shared.Extensions;
using FluentAssertions;
using Xunit;

namespace AuctionService.UnitTests.Extensions;

/// <summary>
/// AuctionExtensions 單元測試
/// </summary>
public class AuctionExtensionsTests
{
    [Fact]
    public void CalculateStatus_ShouldReturnPending_WhenCurrentTimeIsBeforeStartTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            StartTime = now.AddHours(1), // 開始時間在未來
            EndTime = now.AddHours(2)
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be("Pending");
    }

    [Fact]
    public void CalculateStatus_ShouldReturnActive_WhenCurrentTimeIsBetweenStartAndEndTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            StartTime = now.AddHours(-1), // 開始時間在過去
            EndTime = now.AddHours(1)     // 結束時間在未來
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be("Active");
    }

    [Fact]
    public void CalculateStatus_ShouldReturnEnded_WhenCurrentTimeIsAfterEndTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            StartTime = now.AddHours(-2), // 開始時間在過去
            EndTime = now.AddHours(-1)    // 結束時間在過去
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be("Ended");
    }

    [Fact]
    public void CalculateStatus_ShouldReturnActive_WhenCurrentTimeEqualsStartTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            StartTime = now,             // 開始時間等於現在
            EndTime = now.AddHours(1)
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be("Active");
    }

    [Fact]
    public void CalculateStatus_ShouldReturnEnded_WhenCurrentTimeEqualsEndTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            StartTime = now.AddHours(-1),
            EndTime = now                  // 結束時間等於現在
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be("Ended");
    }
}