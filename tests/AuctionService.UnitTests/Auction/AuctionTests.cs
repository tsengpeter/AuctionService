using Auction.Domain.Entities;
using FluentAssertions;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction;

public class AuctionTests
{
    private static AuctionEntity CreateSampleAuction(
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null)
    {
        var start = startTime ?? DateTimeOffset.UtcNow.AddHours(1);
        var end = endTime ?? DateTimeOffset.UtcNow.AddDays(7);
        return AuctionEntity.Create(Guid.NewGuid(), "Test Item", null, 100m, start, end, null, null);
    }

    [Fact]
    public void Create_ShouldReturnAuction_WithActiveStatus()
    {
        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = DateTimeOffset.UtcNow.AddDays(7);
        var auction = AuctionEntity.Create(Guid.NewGuid(), "Test Item", null, 100m, start, end, null, null);

        auction.Title.Should().Be("Test Item");
        auction.StartingPrice.Should().Be(100m);
        auction.Status.Should().Be(AuctionStatus.Active);
        auction.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void End_FromActive_ShouldSetStatusToEnded_WithWinnerInfo()
    {
        var auction = CreateSampleAuction();
        var winnerId = Guid.NewGuid();

        auction.End(winnerId, 150m);

        auction.Status.Should().Be(AuctionStatus.Ended);
        auction.WinnerId.Should().Be(winnerId);
        auction.SoldAmount.Should().Be(150m);
    }

    [Fact]
    public void End_WhenNotActive_ShouldThrow()
    {
        var auction = CreateSampleAuction();
        auction.End(null, null);
        var act = () => auction.End(null, null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateAll_BeforeBiddingOpens_ShouldUpdateAllFields()
    {
        var auction = CreateSampleAuction(startTime: DateTimeOffset.UtcNow.AddHours(2));
        var newStart = DateTimeOffset.UtcNow.AddHours(3);
        var newEnd = DateTimeOffset.UtcNow.AddDays(10);

        auction.UpdateAll("New Title", "Desc", 200m, newStart, newEnd, null, null);

        auction.Title.Should().Be("New Title");
        auction.StartingPrice.Should().Be(200m);
        auction.StartTime.Should().Be(newStart);
    }

    [Fact]
    public void UpdateNonSensitive_AfterBiddingOpens_ShouldUpdateTitleOnly()
    {
        var auction = CreateSampleAuction(startTime: DateTimeOffset.UtcNow.AddSeconds(-1));

        auction.UpdateNonSensitive("Updated Title", null, null, null);

        auction.Title.Should().Be("Updated Title");
    }

    [Fact]
    public void UpdateAll_AfterBiddingOpens_ShouldThrow()
    {
        var auction = CreateSampleAuction(startTime: DateTimeOffset.UtcNow.AddSeconds(-1));

        var act = () => auction.UpdateAll("New Title", null, 200m,
            DateTimeOffset.UtcNow.AddHours(3), DateTimeOffset.UtcNow.AddDays(10), null, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bidding has started*");
    }
}
