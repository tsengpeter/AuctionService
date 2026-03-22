using AuctionService.IntegrationTests.Infrastructure;
using Bidding.Domain.Entities;
using Bidding.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests.Bidding;

[Collection("Integration")]
public class BiddingDbContextTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void DbContext_ShouldUseSchema_Bidding()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BiddingDbContext>();

        var entity = db.Model.FindEntityType(typeof(Bid));
        entity!.GetSchema().Should().Be("bidding");
    }

    [Fact]
    public async Task DbContext_ShouldCreateAndReadBid()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BiddingDbContext>();

        var bid = Bid.Place(Guid.NewGuid(), Guid.NewGuid(), 250m);

        db.Bids.Add(bid);
        await db.SaveChangesAsync();

        var loaded = await db.Bids.FindAsync(bid.Id);
        loaded.Should().NotBeNull();
        loaded!.Amount.Should().Be(250m);
    }
}
