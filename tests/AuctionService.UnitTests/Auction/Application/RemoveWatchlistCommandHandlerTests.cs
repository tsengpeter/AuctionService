using Auction.Application.Commands.RemoveWatchlist;
using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction.Application;

public class RemoveWatchlistCommandHandlerTests : IAsyncLifetime
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

    private async Task<(AuctionEntity Auction, Watchlist Entry)> SeedWatchlistEntryAsync(Guid userId)
    {
        var auction = AuctionEntity.Create(Guid.NewGuid(), "Remove Test", null, 50m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(3), null, null);
        _db.Auctions.Add(auction);
        var entry = Watchlist.Create(userId, auction.Id);
        _db.Watchlist.Add(entry);
        await _db.SaveChangesAsync();
        return (auction, entry);
    }

    [Fact]
    public async Task Handle_ExistingEntry_RemovesFromWatchlist()
    {
        var userId = Guid.NewGuid();
        var (auction, _) = await SeedWatchlistEntryAsync(userId);
        var handler = new RemoveWatchlistCommandHandler(_db);

        await handler.Handle(new RemoveWatchlistCommand(userId, auction.Id), CancellationToken.None);

        var exists = await _db.Watchlist.AnyAsync(w => w.UserId == userId && w.AuctionId == auction.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonExistentEntry_IsIdempotent()
    {
        var handler = new RemoveWatchlistCommandHandler(_db);
        var act = () => handler.Handle(
            new RemoveWatchlistCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
