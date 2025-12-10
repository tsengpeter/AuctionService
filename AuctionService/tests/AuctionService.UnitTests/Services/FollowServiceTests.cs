using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Exceptions;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Services;
using AuctionService.Core.DTOs.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuctionService.UnitTests.Services;

/// <summary>
/// FollowService 單元測試
/// </summary>
public class FollowServiceTests
{
    private readonly Mock<IFollowRepository> _followRepositoryMock;
    private readonly Mock<IAuctionRepository> _auctionRepositoryMock;
    private readonly Mock<IBiddingServiceClient> _biddingServiceClientMock;
    private readonly FollowService _followService;

    public FollowServiceTests()
    {
        _followRepositoryMock = new Mock<IFollowRepository>();
        _auctionRepositoryMock = new Mock<IAuctionRepository>();
        _biddingServiceClientMock = new Mock<IBiddingServiceClient>();

        _followService = new FollowService(
            _followRepositoryMock.Object,
            _auctionRepositoryMock.Object,
            _biddingServiceClientMock.Object);
    }

    [Fact]
    public async Task AddFollowAsync_WithValidData_ReturnsFollowDto()
    {
        // Arrange
        var userId = "test-user";
        var auctionId = Guid.NewGuid();
        var auction = new Auction
        {
            Id = auctionId,
            Name = "Test Auction",
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            UserId = "other-user"
        };

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync(auction);

        _followRepositoryMock
            .Setup(x => x.ExistsAsync(userId, auctionId))
            .ReturnsAsync(false);

        _followRepositoryMock
            .Setup(x => x.IsFollowingOwnAuctionAsync(userId, auctionId))
            .ReturnsAsync(false);

        _followRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<AuctionQueryParameters>()))
            .ReturnsAsync((new List<Follow>().AsEnumerable(), 0));

        _biddingServiceClientMock
            .Setup(x => x.GetCurrentBidAsync(auctionId))
            .ReturnsAsync((CurrentBidDto?)null);

        var savedFollow = new Follow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AuctionId = auctionId,
            CreatedAt = DateTime.UtcNow,
            Auction = auction
        };

        _followRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Follow>()))
            .ReturnsAsync(savedFollow);

        // Act
        var result = await _followService.AddFollowAsync(userId, auctionId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.AuctionId.Should().Be(auctionId);
        result.AuctionName.Should().Be(auction.Name);
    }

    [Fact]
    public async Task AddFollowAsync_WithNonExistentAuction_ThrowsAuctionNotFoundException()
    {
        // Arrange
        var userId = "test-user";
        var auctionId = Guid.NewGuid();

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync((Auction?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AuctionNotFoundException>(() =>
            _followService.AddFollowAsync(userId, auctionId));
    }

    [Fact]
    public async Task AddFollowAsync_WithAlreadyFollowing_ThrowsValidationException()
    {
        // Arrange
        var userId = "test-user";
        var auctionId = Guid.NewGuid();
        var auction = new Auction
        {
            Id = auctionId,
            Name = "Test Auction",
            UserId = "other-user"
        };

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync(auction);

        _followRepositoryMock
            .Setup(x => x.ExistsAsync(userId, auctionId))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _followService.AddFollowAsync(userId, auctionId));

        exception.Message.Should().Contain("Already following");
    }

    [Fact]
    public async Task AddFollowAsync_WithOwnAuction_ThrowsValidationException()
    {
        // Arrange
        var userId = "test-user";
        var auctionId = Guid.NewGuid();
        var auction = new Auction
        {
            Id = auctionId,
            Name = "Test Auction",
            UserId = userId // Same user
        };

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync(auction);

        _followRepositoryMock
            .Setup(x => x.ExistsAsync(userId, auctionId))
            .ReturnsAsync(false);

        _followRepositoryMock
            .Setup(x => x.IsFollowingOwnAuctionAsync(userId, auctionId))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _followService.AddFollowAsync(userId, auctionId));

        exception.Message.Should().Contain("Cannot follow your own");
    }

    [Fact]
    public async Task GetUserFollowsAsync_ReturnsPagedResult()
    {
        // Arrange
        var userId = "test-user";
        var parameters = new AuctionQueryParameters { PageNumber = 1, PageSize = 10 };
        var follows = new List<Follow>
        {
            new Follow
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AuctionId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Auction = new Auction
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Auction",
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(1)
                }
            }
        };

        _followRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, parameters))
            .ReturnsAsync((follows, 1));

        _biddingServiceClientMock
            .Setup(x => x.GetCurrentBidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CurrentBidDto?)null);

        // Act
        var result = await _followService.GetUserFollowsAsync(userId, parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task RemoveFollowAsync_WithValidData_RemovesSuccessfully()
    {
        // Arrange
        var userId = "test-user";
        var auctionId = Guid.NewGuid();

        _followRepositoryMock
            .Setup(x => x.ExistsAsync(userId, auctionId))
            .ReturnsAsync(true);

        _followRepositoryMock
            .Setup(x => x.RemoveAsync(userId, auctionId))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await _followService.RemoveFollowAsync(userId, auctionId);

        _followRepositoryMock.Verify(x => x.RemoveAsync(userId, auctionId), Times.Once);
    }

    [Fact]
    public async Task CheckFollowExistsAsync_ReturnsCorrectResult()
    {
        // Arrange
        var userId = "test-user";
        var auctionId = Guid.NewGuid();

        _followRepositoryMock
            .Setup(x => x.ExistsAsync(userId, auctionId))
            .ReturnsAsync(true);

        // Act
        var result = await _followService.CheckFollowExistsAsync(userId, auctionId);

        // Assert
        result.Should().BeTrue();
    }
}