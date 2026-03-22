using Auction.Domain.Entities;
using FluentAssertions;

namespace AuctionService.UnitTests.Auction;

public class AuctionItemTests
{
    [Fact]
    public void Create_ShouldReturnAuctionItem_WithPendingStatus()
    {
        var endsAt = DateTimeOffset.UtcNow.AddDays(7);
        var item = AuctionItem.Create("Test Item", 100m, endsAt);

        item.Title.Should().Be("Test Item");
        item.StartingPrice.Should().Be(100m);
        item.Status.Should().Be(AuctionStatus.Pending);
        item.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WhenTitleIsNullOrWhitespace_ShouldThrow(string? title)
    {
        var act = () => AuctionItem.Create(title!, 100m, DateTimeOffset.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WhenStartingPriceIsNotPositive_ShouldThrow(decimal price)
    {
        var act = () => AuctionItem.Create("Title", price, DateTimeOffset.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>().WithParameterName("startingPrice");
    }

    [Fact]
    public void Create_WhenEndsAtIsInPast_ShouldThrow()
    {
        var act = () => AuctionItem.Create("Title", 100m, DateTimeOffset.UtcNow.AddMinutes(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("endsAt");
    }

    [Fact]
    public void Activate_FromPending_ShouldSetStatusToActive()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Activate();
        item.Status.Should().Be(AuctionStatus.Active);
    }

    [Fact]
    public void Activate_WhenNotPending_ShouldThrow()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Activate();
        var act = () => item.Activate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromActive_ShouldSetStatusToCancelled()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Activate();
        item.Cancel();
        item.Status.Should().Be(AuctionStatus.Cancelled);
    }

    [Fact]
    public void End_FromActive_ShouldSetStatusToEnded()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Activate();
        item.End();
        item.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public void End_WhenNotActive_ShouldThrow()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        var act = () => item.End();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkSold_FromEnded_ShouldSetStatusToSold()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Activate();
        item.End();
        item.MarkSold();
        item.Status.Should().Be(AuctionStatus.Sold);
    }

    [Fact]
    public void MarkSold_WhenNotEnded_ShouldThrow()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Activate();
        var act = () => item.MarkSold();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenSold_ShouldThrow()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Activate();
        item.End();
        item.MarkSold();
        var act = () => item.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var item = AuctionItem.Create("Item", 50m, DateTimeOffset.UtcNow.AddDays(1));
        item.Cancel();
        var act = () => item.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }
}
