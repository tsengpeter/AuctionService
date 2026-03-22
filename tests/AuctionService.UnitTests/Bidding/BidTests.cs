using Bidding.Domain.Entities;
using FluentAssertions;

namespace AuctionService.UnitTests.Bidding;

public class BidTests
{
    [Fact]
    public void Place_ShouldReturnBid_WithCorrectProperties()
    {
        var auctionId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();

        var bid = Bid.Place(auctionId, bidderId, 150m);

        bid.AuctionId.Should().Be(auctionId);
        bid.BidderId.Should().Be(bidderId);
        bid.Amount.Should().Be(150m);
        bid.PlacedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        bid.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Place_WhenAuctionIdIsEmpty_ShouldThrow()
    {
        var act = () => Bid.Place(Guid.Empty, Guid.NewGuid(), 100m);
        act.Should().Throw<ArgumentException>().WithParameterName("auctionId");
    }

    [Fact]
    public void Place_WhenBidderIdIsEmpty_ShouldThrow()
    {
        var act = () => Bid.Place(Guid.NewGuid(), Guid.Empty, 100m);
        act.Should().Throw<ArgumentException>().WithParameterName("bidderId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Place_WhenAmountIsNotPositive_ShouldThrow(decimal amount)
    {
        var act = () => Bid.Place(Guid.NewGuid(), Guid.NewGuid(), amount);
        act.Should().Throw<ArgumentException>().WithParameterName("amount");
    }
}
