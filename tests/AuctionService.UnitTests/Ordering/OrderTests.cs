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
    public void Create_WhenBuyerIdIsEmpty_ShouldThrow()
    {
        var act = () => Order.Create(Guid.NewGuid(), Guid.Empty, 100m);
        act.Should().Throw<ArgumentException>().WithParameterName("buyerId");
    }

    [Fact]
    public void Confirm_WhenNotPending_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        var act = () => order.Confirm();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromPending_ShouldSetStatusToCancelled()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldSetStatusToCancelled()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Cancel();
        var act = () => order.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ship_FromConfirmed_ShouldSetStatusToShipped()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        order.Ship();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_WhenNotConfirmed_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var act = () => order.Ship();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_FromShipped_ShouldSetStatusToCompleted()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        order.Ship();
        order.Complete();
        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void Complete_WhenNotShipped_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        var act = () => order.Complete();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenShipped_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        order.Ship();
        var act = () => order.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.Confirm();
        order.Ship();
        order.Complete();
        var act = () => order.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }
}
