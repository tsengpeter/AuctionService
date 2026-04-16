using Auction.Application.Commands.UpdateAuction;
using Auction.Infrastructure.Persistence;
using AuctionService.Shared.Exceptions;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction.Application;

public class UpdateAuctionCommandHandlerTests : IAsyncLifetime
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

    private async Task<AuctionEntity> SeedAuctionAsync(
        Guid ownerId,
        DateTimeOffset? startTime = null,
        bool ended = false)
    {
        var auction = AuctionEntity.Create(ownerId, "Original Title", "Desc", 100m,
            startTime ?? DateTimeOffset.UtcNow.AddHours(2),
            DateTimeOffset.UtcNow.AddDays(7), null, null);
        if (ended) auction.End(null, null);
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();
        return auction;
    }

    [Fact]
    public async Task Handle_BeforeStartTime_UpdatesAllFields()
    {
        var ownerId = Guid.NewGuid();
        var auction = await SeedAuctionAsync(ownerId, DateTimeOffset.UtcNow.AddHours(2));
        var handler = new UpdateAuctionCommandHandler(_db);
        var command = new UpdateAuctionCommand(auction.Id, ownerId,
            "New Title", "New Desc", 200m,
            DateTimeOffset.UtcNow.AddHours(3), DateTimeOffset.UtcNow.AddDays(10),
            null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("New Title");
        result.StartingPrice.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_BeforeStartTime_UpdatesStartingPriceSuccessfully()
    {
        var ownerId = Guid.NewGuid();
        var auction = await SeedAuctionAsync(ownerId, DateTimeOffset.UtcNow.AddHours(2));
        var handler = new UpdateAuctionCommandHandler(_db);
        var command = new UpdateAuctionCommand(auction.Id, ownerId,
            null, null, 999m, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.StartingPrice.Should().Be(999m);
    }

    [Fact]
    public async Task Handle_AfterStartTime_UpdatesTitleSuccessfully()
    {
        var ownerId = Guid.NewGuid();
        // StartTime is in the past → bidding has started
        var auction = await SeedAuctionAsync(ownerId, DateTimeOffset.UtcNow.AddHours(-1));
        var handler = new UpdateAuctionCommandHandler(_db);
        var command = new UpdateAuctionCommand(auction.Id, ownerId,
            "Updated Title", null, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task Handle_AfterStartTime_StartingPriceChange_ThrowsValidationException()
    {
        var ownerId = Guid.NewGuid();
        var auction = await SeedAuctionAsync(ownerId, DateTimeOffset.UtcNow.AddHours(-1));
        var handler = new UpdateAuctionCommandHandler(_db);
        var command = new UpdateAuctionCommand(auction.Id, ownerId,
            null, null, 500m, null, null, null, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_EndedAuction_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var auction = await SeedAuctionAsync(ownerId, ended: true);
        var handler = new UpdateAuctionCommandHandler(_db);
        var command = new UpdateAuctionCommand(auction.Id, ownerId,
            "New Title", null, null, null, null, null, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_NonOwner_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var auction = await SeedAuctionAsync(ownerId);
        var handler = new UpdateAuctionCommandHandler(_db);
        var command = new UpdateAuctionCommand(auction.Id, anotherUserId,
            "New Title", null, null, null, null, null, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_NonExistentAuction_ThrowsNotFoundException()
    {
        var handler = new UpdateAuctionCommandHandler(_db);
        var command = new UpdateAuctionCommand(Guid.NewGuid(), Guid.NewGuid(),
            "Title", null, null, null, null, null, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
