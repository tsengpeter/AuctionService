using BiddingService.Api.Controllers;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Exceptions;
using BiddingService.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BiddingService.UnitTests.Controllers;

public class BidsControllerTests
{
    private readonly Mock<IBiddingService> _biddingServiceMock;
    private readonly BidsController _controller;

    public BidsControllerTests()
    {
        _biddingServiceMock = new Mock<IBiddingService>();
        _controller = new BidsController(_biddingServiceMock.Object);
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
    public async Task GetBidById_WhenBidDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var bidId = 999999L;
        _biddingServiceMock
            .Setup(x => x.GetBidByIdAsync(bidId))
            .ThrowsAsync(new BidNotFoundException(bidId));

        // Act
        var result = await _controller.GetBidById(bidId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBidById_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var bidId = 123456789L;
        _biddingServiceMock
            .Setup(x => x.GetBidByIdAsync(bidId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBidById(bidId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }
}
