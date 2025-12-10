using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Services;
using FluentAssertions;
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
            UserId = Guid.NewGuid(),
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