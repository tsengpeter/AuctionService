using Auction.Application.Queries.GetWatchlist;
using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction.Application;

public class GetWatchlistQueryHandlerTests : IAsyncLifetime
{
    private AuctionDbContext _db = null!;
    private Guid _userId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AuctionDbContext(options);
        await SeedDataAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task SeedDataAsync()
    {
        var activeAuction = AuctionEntity.Create(Guid.NewGuid(), "Active Item", null, 100m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7), null, null);
        var endedAuction = AuctionEntity.Create(Guid.NewGuid(), "Ended Item", null, 100m,
            DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddDays(-1), null, null);
        endedAuction.End(null, null);

        _db.Auctions.AddRange(activeAuction, endedAuction);
        _db.Watchlist.AddRange(
            Watchlist.Create(_userId, activeAuction.Id),
            Watchlist.Create(_userId, endedAuction.Id));
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_NullStatus_ReturnsAllWatchlistItems()
    {
        var handler = new GetWatchlistQueryHandler(_db);

        var result = await handler.Handle(new GetWatchlistQuery(_userId, null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_StatusActive_ReturnsOnlyActiveAuctions()
    {
        var handler = new GetWatchlistQueryHandler(_db);

        var result = await handler.Handle(new GetWatchlistQuery(_userId, "active"), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Status.Should().Be("Active");
    }
}
