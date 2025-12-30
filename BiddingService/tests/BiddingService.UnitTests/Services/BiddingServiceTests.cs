using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Entities;
using BiddingService.Core.Exceptions;
using BiddingService.Core.Interfaces;
using BiddingService.Core.Services;
using BiddingService.Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BiddingService.UnitTests.Services;

public class BiddingServiceTests
{
    private readonly Mock<IBidRepository> _bidRepositoryMock;
    private readonly Mock<IRedisRepository> _redisRepositoryMock;
    private readonly Mock<IAuctionServiceClient> _auctionServiceClientMock;
    private readonly Mock<ISnowflakeIdGenerator> _idGeneratorMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<ILogger<BiddingService.Core.Services.BiddingService>> _loggerMock;
    private readonly BiddingService.Core.Services.BiddingService _service;

    public BiddingServiceTests()
    {
        _bidRepositoryMock = new Mock<IBidRepository>();
        _redisRepositoryMock = new Mock<IRedisRepository>();
        _auctionServiceClientMock = new Mock<IAuctionServiceClient>();
        _idGeneratorMock = new Mock<ISnowflakeIdGenerator>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _loggerMock = new Mock<ILogger<BiddingService.Core.Services.BiddingService>>();

        _service = new BiddingService.Core.Services.BiddingService(
            _bidRepositoryMock.Object,
            _redisRepositoryMock.Object,
            _auctionServiceClientMock.Object,
            _idGeneratorMock.Object,
            _encryptionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateBidAsync_WhenAuctionDoesNotExist_ThrowsAuctionNotFoundException()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100 };
        var bidderId = "bidder123";

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(request.AuctionId))
            .ReturnsAsync((AuctionInfo)null);

        // Act
        Func<Task> act = async () => await _service.CreateBidAsync(request, bidderId);

        // Assert
        await act.Should().ThrowAsync<AuctionNotFoundException>()
            .WithMessage($"Auction with ID {request.AuctionId} not found");
    }

    [Fact]
    public async Task CreateBidAsync_WhenAuctionIsNotActive_ThrowsAuctionNotActiveException()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100 };
        var bidderId = "bidder123";
        var auction = new AuctionInfo { Id = 1, IsActive = false };

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(request.AuctionId))
            .ReturnsAsync(auction);

        // Act
        Func<Task> act = async () => await _service.CreateBidAsync(request, bidderId);

        // Assert
        await act.Should().ThrowAsync<AuctionNotActiveException>()
            .WithMessage($"Auction with ID {request.AuctionId} is not active");
    }

    [Fact]
    public async Task CreateBidAsync_WhenBidderAlreadyHasBid_ThrowsDuplicateBidException()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100 };
        var bidderId = "bidder123";
        var auction = new AuctionInfo { Id = 1, IsActive = true };
        var existingBid = new Bid(123, 1, "encrypted", "hash", new BidAmount(50), DateTime.UtcNow);

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(request.AuctionId))
            .ReturnsAsync(auction);

        _redisRepositoryMock
            .Setup(x => x.GetBidAsync(request.AuctionId, bidderId))
            .ReturnsAsync(existingBid);

        // Act
        Func<Task> act = async () => await _service.CreateBidAsync(request, bidderId);

        // Assert
        await act.Should().ThrowAsync<DuplicateBidException>()
            .WithMessage($"Bidder {bidderId} has already placed a bid on auction {request.AuctionId}");
    }

    [Fact]
    public async Task CreateBidAsync_WhenBidAmountTooLow_ThrowsBidAmountTooLowException()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 50 };
        var bidderId = "bidder123";
        var auction = new AuctionInfo { Id = 1, IsActive = true };
        var highestBid = new Bid(123, 1, "encrypted", "hash", new BidAmount(100), DateTime.UtcNow);

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(request.AuctionId))
            .ReturnsAsync(auction);

        _redisRepositoryMock
            .Setup(x => x.GetBidAsync(request.AuctionId, bidderId))
            .ReturnsAsync((Bid)null);

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(request.AuctionId))
            .ReturnsAsync(highestBid);

        // Act
        Func<Task> act = async () => await _service.CreateBidAsync(request, bidderId);

        // Assert
        await act.Should().ThrowAsync<BidAmountTooLowException>()
            .WithMessage($"Bid amount 50 is too low. Current highest bid is 100");
    }

    [Fact]
    public async Task CreateBidAsync_WhenRedisPlaceBidFails_ThrowsBidAmountTooLowException()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 150 };
        var bidderId = "bidder123";
        var auction = new AuctionInfo { Id = 1, IsActive = true };
        var highestBid = new Bid(123, 1, "encrypted", "hash", new BidAmount(100), DateTime.UtcNow);

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(request.AuctionId))
            .ReturnsAsync(auction);

        _redisRepositoryMock
            .Setup(x => x.GetBidAsync(request.AuctionId, bidderId))
            .ReturnsAsync((Bid)null);

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(request.AuctionId))
            .ReturnsAsync(highestBid);

        _idGeneratorMock
            .Setup(x => x.GenerateId())
            .Returns(456);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(bidderId))
            .Returns("encrypted_bidder");

        _redisRepositoryMock
            .Setup(x => x.PlaceBidAsync(It.IsAny<Bid>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(false); // Redis placement fails

        // Act
        Func<Task> act = async () => await _service.CreateBidAsync(request, bidderId);

        // Assert
        await act.Should().ThrowAsync<BidAmountTooLowException>();
    }

    [Fact]
    public async Task GetBidHistoryAsync_WhenCalled_ReturnsBidHistoryResponse()
    {
        // Arrange
        var auctionId = 1L;
        var page = 1;
        var pageSize = 10;
        var bids = new List<Bid>
        {
            new Bid(1, auctionId, "bidder1", "hash1", new BidAmount(100), DateTime.UtcNow),
            new Bid(2, auctionId, "bidder2", "hash2", new BidAmount(150), DateTime.UtcNow)
        };

        _bidRepositoryMock
            .Setup(x => x.GetBidsByAuctionAsync(auctionId, page, pageSize))
            .ReturnsAsync(bids);

        _bidRepositoryMock
            .Setup(x => x.GetBidCountAsync(auctionId))
            .ReturnsAsync(2);

        // Act
        var result = await _service.GetBidHistoryAsync(auctionId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.AuctionId.Should().Be(auctionId);
        result.Bids.Should().HaveCount(2);
        result.Pagination.CurrentPage.Should().Be(page);
        result.Pagination.PageSize.Should().Be(pageSize);
        result.Pagination.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetMyBidsAsync_WhenCalled_ReturnsMyBidsResponse()
    {
        // Arrange
        var bidderId = "test-bidder";
        var bidderIdHash = "433135eb609d0bf0b9f123fae9e5d5a6b21cbdb712a05468c3191cdeb3e1ab6d";
        var page = 1;
        var pageSize = 10;
        var auctionId = 1L;

        var bids = new List<Bid>
        {
            new Bid(1, auctionId, "bidder1", bidderIdHash, new BidAmount(100), DateTime.UtcNow)
        };

        var auctions = new List<AuctionInfo>
        {
            new AuctionInfo { Id = auctionId, Title = "Test Auction", IsActive = true }
        };

        var highestBid = new Bid(2, auctionId, "bidder2", "hash2", new BidAmount(150), DateTime.UtcNow);

        _bidRepositoryMock
            .Setup(x => x.GetBidsByBidderIdHashAsync(bidderIdHash, page, pageSize))
            .ReturnsAsync(bids);

        _bidRepositoryMock
            .Setup(x => x.GetBidsCountByBidderIdHashAsync(bidderIdHash))
            .ReturnsAsync(1);

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionsBatchAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(auctions);

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync(highestBid);

        // Act
        var result = await _service.GetMyBidsAsync(bidderId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Bids.Should().HaveCount(1);
        result.Bids.First().BidId.Should().Be(1);
        result.Bids.First().AuctionId.Should().Be(auctionId);
        result.Bids.First().AuctionTitle.Should().Be("Test Auction");
        result.Bids.First().Amount.Should().Be(100);
        result.Bids.First().IsHighestBid.Should().BeFalse(); // 100 < 150
        result.Bids.First().IsAuctionActive.Should().BeTrue();
        result.Pagination.CurrentPage.Should().Be(page);
        result.Pagination.PageSize.Should().Be(pageSize);
        result.Pagination.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyBidsAsync_WhenNoBids_ReturnsEmptyResponse()
    {
        // Arrange
        var bidderId = "test-bidder";
        var bidderIdHash = "hashed-bidder";
        var page = 1;
        var pageSize = 10;
        var emptyBids = new List<Bid>();

        _bidRepositoryMock
            .Setup(x => x.GetBidsByBidderIdHashAsync(bidderIdHash, page, pageSize))
            .ReturnsAsync(emptyBids);

        _bidRepositoryMock
            .Setup(x => x.GetBidsCountByBidderIdHashAsync(bidderIdHash))
            .ReturnsAsync(0);

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionsBatchAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<AuctionInfo>());

        // Act
        var result = await _service.GetMyBidsAsync(bidderId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Bids.Should().BeEmpty();
        result.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHighestBidAsync_WhenHighestBidExistsInRedis_ReturnsHighestBidResponse()
    {
        // Arrange
        var auctionId = 1L;
        var highestBid = new Bid(1, auctionId, "bidder1", "hash1", new BidAmount(150), DateTime.UtcNow);

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync(highestBid);

        // Act
        var result = await _service.GetHighestBidAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result.AuctionId.Should().Be(auctionId);
        result.HighestBid.Should().NotBeNull();
        result.HighestBid.BidId.Should().Be(highestBid.BidId);
        result.HighestBid.AuctionId.Should().Be(highestBid.AuctionId);
        result.HighestBid.Amount.Should().Be(highestBid.Amount.Value);
        result.HighestBid.BidAt.Should().Be(highestBid.BidAt);

        _redisRepositoryMock.Verify(x => x.GetHighestBidAsync(auctionId), Times.Once);
        _bidRepositoryMock.Verify(x => x.GetHighestBidAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task GetHighestBidAsync_WhenHighestBidNotInRedis_FallsBackToDatabase()
    {
        // Arrange
        var auctionId = 1L;
        var dbHighestBid = new Bid(2, auctionId, "bidder2", "hash2", new BidAmount(200), DateTime.UtcNow);

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync((Bid?)null);

        _bidRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync(dbHighestBid);

        // Act
        var result = await _service.GetHighestBidAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result.AuctionId.Should().Be(auctionId);
        result.HighestBid.Should().NotBeNull();
        result.HighestBid.BidId.Should().Be(dbHighestBid.BidId);
        result.HighestBid.AuctionId.Should().Be(dbHighestBid.AuctionId);
        result.HighestBid.Amount.Should().Be(dbHighestBid.Amount.Value);
        result.HighestBid.BidAt.Should().Be(dbHighestBid.BidAt);

        _redisRepositoryMock.Verify(x => x.GetHighestBidAsync(auctionId), Times.Once);
        _bidRepositoryMock.Verify(x => x.GetHighestBidAsync(auctionId), Times.Once);
    }

    [Fact]
    public async Task GetHighestBidAsync_WhenNoHighestBidExists_ReturnsNullHighestBid()
    {
        // Arrange
        var auctionId = 1L;

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync((Bid?)null);

        _bidRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync((Bid?)null);

        // Act
        var result = await _service.GetHighestBidAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result.AuctionId.Should().Be(auctionId);
        result.HighestBid.Should().BeNull();

        _redisRepositoryMock.Verify(x => x.GetHighestBidAsync(auctionId), Times.Once);
        _bidRepositoryMock.Verify(x => x.GetHighestBidAsync(auctionId), Times.Once);
    }

    [Fact]
    public async Task GetAuctionStatsAsync_WhenAuctionExists_ReturnsAuctionStatsResponse()
    {
        // Arrange
        var auctionId = 1L;
        var auction = new AuctionInfo { Id = auctionId, Title = "Test Auction", StartingPrice = 100m, IsActive = true };
        var highestBid = new Bid(1, auctionId, "bidder1", "hash1", new BidAmount(150), DateTime.UtcNow);
        var statsData = new AuctionStatsData
        {
            TotalBids = 10,
            UniqueBidders = 5,
            AverageBidAmount = 125m,
            FirstBidAt = DateTime.UtcNow.AddHours(-2),
            LastBidAt = DateTime.UtcNow.AddMinutes(-30),
            BidsInLastHour = 3,
            BidsInLast24Hours = 8
        };

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(auctionId))
            .ReturnsAsync(auction);

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync(highestBid);

        _bidRepositoryMock
            .Setup(x => x.GetAuctionStatsAsync(auctionId))
            .ReturnsAsync(statsData);

        // Act
        var result = await _service.GetAuctionStatsAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result.AuctionId.Should().Be(auctionId);
        result.TotalBids.Should().Be(statsData.TotalBids);
        result.UniqueBidders.Should().Be(statsData.UniqueBidders);
        result.StartingPrice.Should().Be(auction.StartingPrice);
        result.CurrentHighestBid.Should().Be(highestBid.Amount.Value);
        result.AverageBidAmount.Should().Be(statsData.AverageBidAmount);
        result.FirstBidAt.Should().Be(statsData.FirstBidAt);
        result.LastBidAt.Should().Be(statsData.LastBidAt);
        result.BidsInLastHour.Should().Be(statsData.BidsInLastHour);
        result.BidsInLast24Hours.Should().Be(statsData.BidsInLast24Hours);

        // Price growth rate: ((150 - 100) / 100) * 100 = 50%
        result.PriceGrowthRate.Should().Be(50m);

        _auctionServiceClientMock.Verify(x => x.GetAuctionAsync(auctionId), Times.Once);
        _redisRepositoryMock.Verify(x => x.GetHighestBidAsync(auctionId), Times.Once);
        _bidRepositoryMock.Verify(x => x.GetAuctionStatsAsync(auctionId), Times.Once);
    }

    [Fact]
    public async Task GetAuctionStatsAsync_WhenAuctionNotFound_ThrowsAuctionNotFoundException()
    {
        // Arrange
        var auctionId = 1L;

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(auctionId))
            .ReturnsAsync((AuctionInfo?)null);

        // Act
        Func<Task> act = async () => await _service.GetAuctionStatsAsync(auctionId);

        // Assert
        await act.Should().ThrowAsync<AuctionNotFoundException>()
            .WithMessage($"Auction with ID {auctionId} not found");

        _auctionServiceClientMock.Verify(x => x.GetAuctionAsync(auctionId), Times.Once);
        _redisRepositoryMock.Verify(x => x.GetHighestBidAsync(It.IsAny<long>()), Times.Never);
        _bidRepositoryMock.Verify(x => x.GetAuctionStatsAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task GetAuctionStatsAsync_WhenNoHighestBid_ReturnsNullCurrentHighestBid()
    {
        // Arrange
        var auctionId = 1L;
        var auction = new AuctionInfo { Id = auctionId, Title = "Test Auction", StartingPrice = 100m, IsActive = true };
        var statsData = new AuctionStatsData
        {
            TotalBids = 0,
            UniqueBidders = 0,
            AverageBidAmount = 0m,
            FirstBidAt = null,
            LastBidAt = null,
            BidsInLastHour = 0,
            BidsInLast24Hours = 0
        };

        _auctionServiceClientMock
            .Setup(x => x.GetAuctionAsync(auctionId))
            .ReturnsAsync(auction);

        _redisRepositoryMock
            .Setup(x => x.GetHighestBidAsync(auctionId))
            .ReturnsAsync((Bid?)null);

        _bidRepositoryMock
            .Setup(x => x.GetAuctionStatsAsync(auctionId))
            .ReturnsAsync(statsData);

        // Act
        var result = await _service.GetAuctionStatsAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result.AuctionId.Should().Be(auctionId);
        result.CurrentHighestBid.Should().BeNull();
        result.PriceGrowthRate.Should().BeNull();

        _auctionServiceClientMock.Verify(x => x.GetAuctionAsync(auctionId), Times.Once);
        _redisRepositoryMock.Verify(x => x.GetHighestBidAsync(auctionId), Times.Once);
        _bidRepositoryMock.Verify(x => x.GetAuctionStatsAsync(auctionId), Times.Once);
    }
}