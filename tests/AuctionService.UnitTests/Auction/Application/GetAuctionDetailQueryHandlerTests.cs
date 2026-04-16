using Auction.Application.Abstractions;
using Auction.Application.Queries.GetAuctionDetail;
using Auction.Infrastructure.Persistence;
using AuctionService.Shared.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction.Application;

public class GetAuctionDetailQueryHandlerTests : IAsyncLifetime
{
    private AuctionDbContext _db = null!;
    private IBiddingQueryService _biddingService = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AuctionDbContext(options);
        _biddingService = Substitute.For<IBiddingQueryService>();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<AuctionEntity> SeedAuctionAsync(string title = "Test Auction")
    {
        var auction = AuctionEntity.Create(
            Guid.NewGuid(), title, "Description", 100m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7),
            null, null);
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();
        return auction;
    }

    [Fact]
    public async Task Handle_ExistingAuction_ReturnsFullDto_WithNullBid()
    {
        var auction = await SeedAuctionAsync();
        _biddingService.GetHighestBidAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(null));

        var handler = new GetAuctionDetailQueryHandler(_db, _biddingService);
        var result = await handler.Handle(new GetAuctionDetailQuery(auction.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(auction.Id);
        result.Title.Should().Be("Test Auction");
        result.CurrentHighestBid.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistentAuction_ThrowsNotFoundException()
    {
        _biddingService.GetHighestBidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(null));

        var handler = new GetAuctionDetailQueryHandler(_db, _biddingService);
        var act = () => handler.Handle(new GetAuctionDetailQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CallsIBiddingQueryService()
    {
        var auction = await SeedAuctionAsync();
        _biddingService.GetHighestBidAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(new BidInfoDto(Guid.NewGuid(), 150m)));

        var handler = new GetAuctionDetailQueryHandler(_db, _biddingService);
        var result = await handler.Handle(new GetAuctionDetailQuery(auction.Id), CancellationToken.None);

        await _biddingService.Received(1).GetHighestBidAsync(auction.Id, Arg.Any<CancellationToken>());
        result.CurrentHighestBid.Should().Be(150m);
    }
}
