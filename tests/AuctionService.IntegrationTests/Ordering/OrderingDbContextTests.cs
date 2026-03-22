using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;

namespace AuctionService.IntegrationTests.Ordering;

[Collection("Integration")]
public class OrderingDbContextTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void DbContext_ShouldUseSchema_Ordering()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        var entity = db.Model.FindEntityType(typeof(Order));
        entity!.GetSchema().Should().Be("ordering");
    }

    [Fact]
    public async Task DbContext_ShouldCreateAndReadOrder()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 500m);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var loaded = await db.Orders.FindAsync(order.Id);
        loaded.Should().NotBeNull();
        loaded!.Amount.Should().Be(500m);
    }
}
