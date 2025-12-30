using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Exceptions;
using AuctionService.Core.Extensions;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Services;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace AuctionService.UnitTests.Services;

/// <summary>
/// AuctionService 單元測試
/// </summary>
public class AuctionServiceTests
{
    private readonly Mock<IAuctionRepository> _auctionRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IBiddingServiceClient> _biddingServiceClientMock;
    private readonly AuctionService.Core.Services.AuctionService _auctionService;

    public AuctionServiceTests()
    {
        _auctionRepositoryMock = new Mock<IAuctionRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _biddingServiceClientMock = new Mock<IBiddingServiceClient>();
        _auctionService = new AuctionService.Core.Services.AuctionService(
            _auctionRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _biddingServiceClientMock.Object);
    }

    [Fact]
    public async Task GetAuctionsAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var parameters = new AuctionQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var auctions = new List<Auction>
        {
            CreateTestAuction("Test Auction 1"),
            CreateTestAuction("Test Auction 2")
        };

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionsAsync(parameters))
            .ReturnsAsync((auctions, 2));

        // Act
        var result = await _auctionService.GetAuctionsAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        result.Items.First().Title.Should().Be("Test Auction 1");
    }

    [Fact]
    public async Task GetAuctionsAsync_WithEmptyResult_ReturnsEmptyPagedResult()
    {
        // Arrange
        var parameters = new AuctionQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionsAsync(parameters))
            .ReturnsAsync((new List<Auction>(), 0));

        // Act
        var result = await _auctionService.GetAuctionsAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuctionByIdAsync_WithValidId_ReturnsAuctionDetail()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var auction = CreateTestAuction("Test Auction");
        auction.Id = auctionId;

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync(auction);

        // Act
        var result = await _auctionService.GetAuctionByIdAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(auctionId);
        result.Title.Should().Be("Test Auction");
        result.Status.Should().Be(AuctionStatus.Pending); // 因為測試資料是未開始的拍賣
    }

    [Fact]
    public async Task GetAuctionByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var auctionId = Guid.NewGuid();

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync((Auction?)null);

        // Act
        var result = await _auctionService.GetAuctionByIdAsync(auctionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentBidAsync_WithValidAuctionId_ReturnsCurrentBidInfo()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var auction = CreateTestAuction("Test Auction");
        auction.Id = auctionId;

        var expectedBid = new CurrentBidDto
        {
            AuctionId = auctionId,
            CurrentPrice = 150.00m,
            BidCount = 5,
            LastBidTime = DateTime.UtcNow,
            HighestBidder = new BidderDto { Id = Guid.NewGuid(), Username = "bidder1" }
        };

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync(auction);

        _biddingServiceClientMock
            .Setup(x => x.GetCurrentBidAsync(auctionId))
            .ReturnsAsync(expectedBid);

        // Act
        var result = await _auctionService.GetCurrentBidAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result!.AuctionId.Should().Be(auctionId);
        result.CurrentPrice.Should().Be(150.00m);
        result.BidCount.Should().Be(5);
        result.HighestBidder.Should().NotBeNull();
        result.HighestBidder!.Username.Should().Be("bidder1");
    }

    [Fact]
    public async Task GetCurrentBidAsync_WithInvalidAuctionId_ReturnsNull()
    {
        // Arrange
        var auctionId = Guid.NewGuid();

        _auctionRepositoryMock
            .Setup(x => x.GetAuctionByIdAsync(auctionId))
            .ReturnsAsync((Auction?)null);

        // Act
        var result = await _auctionService.GetCurrentBidAsync(auctionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAuctionAsync_WithValidRequest_ReturnsAuctionDetailDto()
    {
        // Arrange
        var request = new AuctionService.Core.DTOs.Requests.CreateAuctionRequest
        {
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(1),
            EndTime = DateTime.UtcNow.AddHours(2)
        };
        var userId = "test-user-id";
        var expectedAuction = CreateTestAuction("Test Auction");
        expectedAuction.Id = Guid.NewGuid();
        expectedAuction.UserId = userId;

        _auctionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Auction>()))
            .ReturnsAsync(expectedAuction);

        // Act
        var result = await _auctionService.CreateAuctionAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedAuction.Id);
        result.Title.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.StartingPrice.Should().Be(request.StartingPrice);
        result.Category.Should().NotBeNull();
        result.Category.Id.Should().Be(request.CategoryId);
        _auctionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Auction>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAuctionAsync_WithPermissionDenied_ThrowsException()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var request = new AuctionService.Core.DTOs.Requests.UpdateAuctionRequest
        {
            Name = "Updated Auction",
            Description = "Updated Description",
            StartingPrice = 150,
            EndTime = DateTime.UtcNow.AddHours(3)
        };
        var userId = "wrong-user-id";
        var existingAuction = CreateTestAuction("Original Auction");
        existingAuction.Id = auctionId;
        existingAuction.UserId = "owner-user-id"; // 不同的擁有者

        _auctionRepositoryMock
            .Setup(x => x.GetByIdAsync(auctionId))
            .ReturnsAsync(existingAuction);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _auctionService.UpdateAuctionAsync(auctionId, request, userId));
    }

    [Fact]
    public async Task UpdateAuctionAsync_WithExistingBids_ThrowsException()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var request = new AuctionService.Core.DTOs.Requests.UpdateAuctionRequest
        {
            Name = "Updated Auction",
            Description = "Updated Description",
            StartingPrice = 150,
            EndTime = DateTime.UtcNow.AddHours(3)
        };
        var userId = "owner-user-id";
        var existingAuction = CreateTestAuction("Original Auction");
        existingAuction.Id = auctionId;
        existingAuction.UserId = userId;

        _auctionRepositoryMock
            .Setup(x => x.GetByIdAsync(auctionId))
            .ReturnsAsync(existingAuction);

        _biddingServiceClientMock
            .Setup(x => x.CheckAuctionHasBidsAsync(auctionId))
            .ReturnsAsync(true); // 模擬已有出價

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _auctionService.UpdateAuctionAsync(auctionId, request, userId));
    }

    [Fact]
    public async Task DeleteAuctionAsync_WithPermissionDenied_ThrowsException()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var userId = "wrong-user-id";
        var existingAuction = CreateTestAuction("Test Auction");
        existingAuction.Id = auctionId;
        existingAuction.UserId = "owner-user-id"; // 不同的擁有者

        _auctionRepositoryMock
            .Setup(x => x.GetByIdAsync(auctionId))
            .ReturnsAsync(existingAuction);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _auctionService.DeleteAuctionAsync(auctionId, userId));
    }

    [Fact]
    public async Task DeleteAuctionAsync_WithExistingBids_ThrowsException()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var userId = "owner-user-id";
        var existingAuction = CreateTestAuction("Test Auction");
        existingAuction.Id = auctionId;
        existingAuction.UserId = userId;

        _auctionRepositoryMock
            .Setup(x => x.GetByIdAsync(auctionId))
            .ReturnsAsync(existingAuction);

        _biddingServiceClientMock
            .Setup(x => x.CheckAuctionHasBidsAsync(auctionId))
            .ReturnsAsync(true); // 模擬已有出價

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _auctionService.DeleteAuctionAsync(auctionId, userId));
    }

    [Fact]
    public async Task UpdateAuctionAsync_WithEndedAuction_ThrowsException()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var userId = "test-user";
        var request = new UpdateAuctionRequest
        {
            Name = "Updated Auction",
            Description = "Updated Description",
            StartingPrice = 150.00m,
            EndTime = DateTime.UtcNow.AddDays(3)
        };

        var endedAuction = CreateTestAuction("Test Auction");
        endedAuction.UserId = userId; // 設置為測試用戶
        endedAuction.StartTime = DateTime.UtcNow.AddDays(-2); // 開始時間設為昨天
        endedAuction.EndTime = DateTime.UtcNow.AddDays(-1); // 結束時間設為昨天

        _auctionRepositoryMock
            .Setup(x => x.GetByIdAsync(auctionId))
            .ReturnsAsync(endedAuction);

        _biddingServiceClientMock
            .Setup(x => x.CheckAuctionHasBidsAsync(auctionId))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _auctionService.UpdateAuctionAsync(auctionId, request, userId));
    }

    [Fact]
    public async Task DeleteAuctionAsync_WithEndedAuction_ThrowsException()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var userId = "test-user";

        var endedAuction = CreateTestAuction("Test Auction");
        endedAuction.UserId = userId; // 設置為測試用戶
        endedAuction.StartTime = DateTime.UtcNow.AddDays(-2); // 開始時間設為昨天
        endedAuction.EndTime = DateTime.UtcNow.AddDays(-1); // 結束時間設為昨天

        _auctionRepositoryMock
            .Setup(x => x.GetByIdAsync(auctionId))
            .ReturnsAsync(endedAuction);

        _biddingServiceClientMock
            .Setup(x => x.CheckAuctionHasBidsAsync(auctionId))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _auctionService.DeleteAuctionAsync(auctionId, userId));
    }

    private static Auction CreateTestAuction(string name)
    {
        return new Auction
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test Description",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(2),
            UserId = "test-user-id",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Category = new Category
            {
                Id = 1,
                Name = "Test Category",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}