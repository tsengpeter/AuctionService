using Auction.Application.Commands.AddWatchlist;
using Auction.Infrastructure.Persistence;
using AuctionService.Shared.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction.Application;

public class AddWatchlistCommandHandlerTests : IAsyncLifetime
{
    private AuctionDbContext _db = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AuctionDbContext(options);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<AuctionEntity> SeedAuctionAsync()
    {
        var auction = AuctionEntity.Create(Guid.NewGuid(), "Watch Me", null, 50m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(3), null, null);
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();
        return auction;
    }

    [Fact]
    public async Task Handle_NewEntry_AddsWatchlistItem()
    {
        var auction = await SeedAuctionAsync();
        var userId = Guid.NewGuid();
        var handler = new AddWatchlistCommandHandler(_db);

        await handler.Handle(new AddWatchlistCommand(userId, auction.Id), CancellationToken.None);

        var exists = await _db.Watchlist.AnyAsync(w => w.UserId == userId && w.AuctionId == auction.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateEntry_IsIdempotent()
    {
        var auction = await SeedAuctionAsync();
        var userId = Guid.NewGuid();
        var handler = new AddWatchlistCommandHandler(_db);

        await handler.Handle(new AddWatchlistCommand(userId, auction.Id), CancellationToken.None);
        await handler.Handle(new AddWatchlistCommand(userId, auction.Id), CancellationToken.None);

        var count = await _db.Watchlist.CountAsync(w => w.UserId == userId && w.AuctionId == auction.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NonExistentAuction_ThrowsNotFoundException()
    {
        var handler = new AddWatchlistCommandHandler(_db);
        var act = () => handler.Handle(new AddWatchlistCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
