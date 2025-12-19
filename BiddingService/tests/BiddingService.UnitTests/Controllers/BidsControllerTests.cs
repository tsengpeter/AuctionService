using BiddingService.Api.Controllers;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Exceptions;
using BiddingService.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BiddingService.UnitTests.Controllers;

public class BidsControllerTests
{
    private readonly Mock<IBiddingService> _biddingServiceMock;
    private readonly Mock<ILogger<BidsController>> _loggerMock;
    private readonly BidsController _controller;

    public BidsControllerTests()
    {
        _biddingServiceMock = new Mock<IBiddingService>();
        _loggerMock = new Mock<ILogger<BidsController>>();
        _controller = new BidsController(_biddingServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetBidById_WhenBidExists_ReturnsOkWithBidResponse()
    {
        // Arrange
        var bidId = 123456789L;
        var expectedResponse = new BidResponse
        {
            BidId = bidId,
            AuctionId = 1,
            BidderIdHash = "hashed-bidder",
            Amount = 100.50m,
            BidAt = DateTime.UtcNow
        };

        _biddingServiceMock
            .Setup(x => x.GetBidByIdAsync(bidId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetBidById(bidId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BidResponse>().Subject;
        response.BidId.Should().Be(bidId);
        response.AuctionId.Should().Be(1);
        response.BidderIdHash.Should().Be("hashed-bidder");
        response.Amount.Should().Be(100.50m);
    }

    [Fact]
    public async Task GetBidById_WhenBidDoesNotExist_ThrowsBidNotFoundException()
    {
        // Arrange
        var bidId = 999999L;
        _biddingServiceMock
            .Setup(x => x.GetBidByIdAsync(bidId))
            .ThrowsAsync(new BidNotFoundException(bidId));

        // Act
        Func<Task> act = async () => await _controller.GetBidById(bidId);

        // Assert
        await act.Should().ThrowAsync<BidNotFoundException>()
            .WithMessage($"Bid with ID {bidId} not found");
    }

    [Fact]
    public async Task GetBidById_WhenServiceThrowsException_ThrowsException()
    {
        // Arrange
        var bidId = 123456789L;
        _biddingServiceMock
            .Setup(x => x.GetBidByIdAsync(bidId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        Func<Task> act = async () => await _controller.GetBidById(bidId);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Database error");
    }
}
