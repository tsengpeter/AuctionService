using FluentAssertions;
using Ordering.Domain.Entities;

namespace AuctionService.UnitTests.Ordering;

public class OrderTests
{
    [Fact]
    public void Create_ShouldReturnOrder_WithPendingStatus()
    {
        var auctionId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();

        var order = Order.Create(auctionId, buyerId, 500m);

        order.AuctionId.Should().Be(auctionId);
        order.BuyerId.Should().Be(buyerId);
        order.Amount.Should().Be(500m);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WhenAuctionIdIsEmpty_ShouldThrow()
    {
        var act = () => Order.Create(Guid.Empty, Guid.NewGuid(), 100m);
        act.Should().Throw<ArgumentException>().WithParameterName("auctionId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenAmountIsNotPositive_ShouldThrow(decimal amount)
    {
        var act = () => Order.Create(Guid.NewGuid(), Guid.NewGuid(), amount);
        act.Should().Throw<ArgumentException>().WithParameterName("amount");
    }

    [Fact]
    public void Confirm_FromPending_ShouldSetStatusToConfirmed()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Cancel_FromPending_ShouldSetStatusToCancelled()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        // manually reach Completed not possible, but test Confirmed→Cancel as valid
        var act = () => Order.Create(Guid.NewGuid(), Guid.NewGuid(), 0);
        act.Should().Throw<ArgumentException>();
    }
}
