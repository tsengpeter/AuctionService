using AuctionService.Core.Entities;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace AuctionService.UnitTests.Extensions;

/// <summary>
/// AuctionExtensions ?��?測試
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
            StartTime = now.AddHours(1), // ?��??��??�未�?
            EndTime = now.AddHours(2)
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be(AuctionStatus.Pending);
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
            StartTime = now.AddHours(-1), // ?��??��??��???
            EndTime = now.AddHours(1)     // 結�??��??�未�?
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be(AuctionStatus.Active);
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
            StartTime = now.AddHours(-2), // ?��??��??��???
            EndTime = now.AddHours(-1)    // 結�??��??��???
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be(AuctionStatus.Ended);
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
            StartTime = now,             // ?��??��?等於?�在
            EndTime = now.AddHours(1)
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be(AuctionStatus.Active);
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
            EndTime = now                  // 結�??��?等於?�在
        };

        // Act
        var status = auction.CalculateStatus();

        // Assert
        status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public void ToEntity_ShouldCreateAuctionEntity_WithCorrectProperties()
    {
        // Arrange
        var userId = "test-user-123";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = DateTime.UtcNow.AddHours(2);
        var request = new CreateAuctionRequest
        {
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = startTime,
            EndTime = endTime
        };

        // Act
        var auction = request.ToEntity(userId);

        // Assert
        auction.Id.Should().NotBeEmpty();
        auction.Name.Should().Be(request.Name);
        auction.Description.Should().Be(request.Description);
        auction.StartingPrice.Should().Be(request.StartingPrice);
        auction.CategoryId.Should().Be(request.CategoryId);
        auction.StartTime.Should().Be(request.StartTime);
        auction.EndTime.Should().Be(request.EndTime);
        auction.UserId.Should().Be(userId);
        auction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        auction.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToEntity_ShouldUseCurrentTimeAsStartTime_WhenStartTimeIsNull()
    {
        // Arrange
        var userId = "test-user-123";
        var endTime = DateTime.UtcNow.AddHours(2);
        var request = new CreateAuctionRequest
        {
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = null,
            EndTime = endTime
        };

        // Act
        var auction = request.ToEntity(userId);

        // Assert
        auction.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToListItemDto_ShouldMapAuctionToListItemDto()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100.00m,
            StartTime = now.AddHours(-1),
            EndTime = now.AddHours(1),
            UserId = "test-user-123",
            Category = new Category
            {
                Id = 1,
                Name = "Electronics"
            }
        };

        // Act
        var dto = auction.ToListItemDto();

        // Assert
        dto.Id.Should().Be(auction.Id);
        dto.Title.Should().Be(auction.Name);
        dto.Description.Should().Be(auction.Description);
        dto.StartingPrice.Should().Be(auction.StartingPrice);
        dto.CurrentPrice.Should().Be(auction.StartingPrice);
        dto.StartTime.Should().Be(auction.StartTime);
        dto.EndTime.Should().Be(auction.EndTime);
        dto.Status.Should().Be(AuctionStatus.Active);
        dto.Category.Should().NotBeNull();
        dto.Category!.Id.Should().Be(auction.Category.Id);
        dto.Category!.Name.Should().Be(auction.Category.Name);
        dto.Seller.Should().NotBeNull();
        dto.Seller!.Id.Should().Be(auction.UserId);
        dto.Seller!.Username.Should().Be($"User_{auction.UserId}");
    }

    [Fact]
    public void ToListItemDto_ShouldHandleNullCategory()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100.00m,
            StartTime = now.AddHours(-1),
            EndTime = now.AddHours(1),
            UserId = "test-user-123",
            Category = null
        };

        // Act
        var dto = auction.ToListItemDto();

        // Assert
        dto.Category.Should().BeNull();
    }

    [Fact]
    public void ToDetailDto_ShouldMapAuctionToDetailDto()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100.00m,
            StartTime = now.AddHours(-1),
            EndTime = now.AddHours(1),
            UserId = "test-user-123",
            Category = new Category
            {
                Id = 1,
                Name = "Electronics"
            }
        };

        // Act
        var dto = auction.ToDetailDto();

        // Assert
        dto.Id.Should().Be(auction.Id);
        dto.Title.Should().Be(auction.Name);
        dto.Description.Should().Be(auction.Description);
        dto.ImageUrls.Should().BeEmpty();
        dto.StartingPrice.Should().Be(auction.StartingPrice);
        dto.CurrentPrice.Should().Be(auction.StartingPrice);
        dto.StartTime.Should().Be(auction.StartTime);
        dto.EndTime.Should().Be(auction.EndTime);
        dto.Status.Should().Be(AuctionStatus.Active);
        dto.Category.Should().NotBeNull();
        dto.Category!.Id.Should().Be(auction.Category.Id);
        dto.Category!.Name.Should().Be(auction.Category.Name);
        dto.Seller.Should().NotBeNull();
        dto.Seller!.Id.Should().Be(auction.UserId);
        dto.Seller!.Username.Should().Be($"User_{auction.UserId}");
        dto.Bids.Should().BeEmpty();
    }

    [Fact]
    public void ToDetailDto_ShouldHandleNullCategory()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100.00m,
            StartTime = now.AddHours(-1),
            EndTime = now.AddHours(1),
            UserId = "test-user-123",
            Category = null
        };

        // Act
        var dto = auction.ToDetailDto();

        // Assert
        dto.Category.Should().BeNull();
    }
}
