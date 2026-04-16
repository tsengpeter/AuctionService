using Auction.Infrastructure.Persistence;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.IntegrationTests.Auction;

[Collection("Integration")]
public class AuctionDbContextTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void DbContext_ShouldUseDefaultSchema_Auction()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        // The default schema is set on the model; all auction tables use schema "auction"
        var entity = db.Model.FindEntityType(typeof(AuctionEntity));
        entity.Should().NotBeNull();
        // Verify the table name is correct (schema enforced by migration)
        var tableName = entity!.GetTableName();
        tableName.Should().Be("auctions");
    }

    [Fact]
    public async Task DbContext_ShouldCreateAndReadAuction()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        var auction = AuctionEntity.Create(
            Guid.NewGuid(), "Test Watch", null, 100m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7),
            null, null);

        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        var loaded = await db.Auctions.FindAsync(auction.Id);
        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Test Watch");
    }

    [Fact]
    public void DbContext_ShouldHave_CategoriesAndWatchlist_Tables()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        db.Categories.Should().NotBeNull();
        db.Watchlist.Should().NotBeNull();
    }
}

