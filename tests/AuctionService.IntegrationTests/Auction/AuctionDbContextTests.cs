using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests.Auction;

[Collection("Integration")]
public class AuctionDbContextTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void DbContext_ShouldUseSchema_Auction()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        var entity = db.Model.FindEntityType(typeof(AuctionItem));
        entity!.GetSchema().Should().Be("auction");
    }

    [Fact]
    public async Task DbContext_ShouldCreateAndReadAuctionItem()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        var item = AuctionItem.Create("Test Watch", 100m, DateTimeOffset.UtcNow.AddDays(7));

        db.AuctionItems.Add(item);
        await db.SaveChangesAsync();

        var loaded = await db.AuctionItems.FindAsync(item.Id);
        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Test Watch");
    }
}
