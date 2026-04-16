using Auction.Application.Abstractions;
using Auction.Domain.Entities;
using Auction.Domain.Events;
using Auction.Infrastructure.BackgroundServices;
using Auction.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction.Application;

public class AuctionEndBackgroundServiceTests
{
    private static AuctionDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (IServiceScopeFactory, AuctionDbContext, IMediator, IBiddingQueryService) BuildDependencies()
    {
        var db = CreateDb();
        var mediator = Substitute.For<IMediator>();
        var biddingService = Substitute.For<IBiddingQueryService>();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(AuctionDbContext)).Returns(db);
        scope.ServiceProvider.GetService(typeof(IMediator)).Returns(mediator);
        scope.ServiceProvider.GetService(typeof(IBiddingQueryService)).Returns(biddingService);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return (scopeFactory, db, mediator, biddingService);
    }

    [Fact]
    public async Task ProcessAsync_ExpiredActiveAuction_SetsStatusToEnded()
    {
        var (scopeFactory, db, _, biddingService) = BuildDependencies();
        biddingService.GetHighestBidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(null));

        var expired = AuctionEntity.Create(Guid.NewGuid(), "Expired", null, 100m,
            DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddMinutes(-1), null, null);
        db.Auctions.Add(expired);
        await db.SaveChangesAsync();

        var svc = new AuctionEndBackgroundService(scopeFactory, NullLogger<AuctionEndBackgroundService>.Instance);
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        var updated = await db.Auctions.FindAsync(expired.Id);
        updated!.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public async Task ProcessAsync_ExpiredWithBid_PublishesAuctionWonEvent()
    {
        var (scopeFactory, db, mediator, biddingService) = BuildDependencies();
        var winnerId = Guid.NewGuid();
        biddingService.GetHighestBidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(new BidInfoDto(winnerId, 200m)));

        var expired = AuctionEntity.Create(Guid.NewGuid(), "Won Auction", null, 100m,
            DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddMinutes(-1), null, null);
        db.Auctions.Add(expired);
        await db.SaveChangesAsync();

        var svc = new AuctionEndBackgroundService(scopeFactory, NullLogger<AuctionEndBackgroundService>.Instance);
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        await mediator.Received(1).Publish(Arg.Is<AuctionWonEvent>(e =>
            e.AuctionId == expired.Id && e.WinnerId == winnerId && e.SoldAmount == 200m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ExpiredWithoutBid_DoesNotPublishEvent()
    {
        var (scopeFactory, db, mediator, biddingService) = BuildDependencies();
        biddingService.GetHighestBidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(null));

        var expired = AuctionEntity.Create(Guid.NewGuid(), "No Bid", null, 100m,
            DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddMinutes(-1), null, null);
        db.Auctions.Add(expired);
        await db.SaveChangesAsync();

        var svc = new AuctionEndBackgroundService(scopeFactory, NullLogger<AuctionEndBackgroundService>.Instance);
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        await mediator.DidNotReceive().Publish(Arg.Any<AuctionWonEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_AlreadyEndedAuction_NotReprocessed()
    {
        var (scopeFactory, db, mediator, biddingService) = BuildDependencies();
        biddingService.GetHighestBidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(null));

        var ended = AuctionEntity.Create(Guid.NewGuid(), "Already Ended", null, 100m,
            DateTimeOffset.UtcNow.AddHours(-3), DateTimeOffset.UtcNow.AddHours(-2), null, null);
        ended.End(null, null);
        db.Auctions.Add(ended);
        await db.SaveChangesAsync();

        var svc = new AuctionEndBackgroundService(scopeFactory, NullLogger<AuctionEndBackgroundService>.Instance);
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        await mediator.DidNotReceive().Publish(Arg.Any<AuctionWonEvent>(), Arg.Any<CancellationToken>());
        await biddingService.DidNotReceive().GetHighestBidAsync(ended.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FutureActiveAuction_RemainsActive()
    {
        var (scopeFactory, db, _, biddingService) = BuildDependencies();
        biddingService.GetHighestBidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(null));

        var future = AuctionEntity.Create(Guid.NewGuid(), "Future", null, 100m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7), null, null);
        db.Auctions.Add(future);
        await db.SaveChangesAsync();

        var svc = new AuctionEndBackgroundService(scopeFactory, NullLogger<AuctionEndBackgroundService>.Instance);
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        var check = await db.Auctions.FindAsync(future.Id);
        check!.Status.Should().Be(AuctionStatus.Active);
    }

    [Fact]
    public async Task ProcessAsync_BatchOf100OrMore_ProcessesUpTo100()
    {
        var (scopeFactory, db, _, biddingService) = BuildDependencies();
        biddingService.GetHighestBidAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BidInfoDto?>(null));

        for (int i = 0; i < 105; i++)
        {
            var a = AuctionEntity.Create(Guid.NewGuid(), $"Expired {i}", null, 100m,
                DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddMinutes(-1), null, null);
            db.Auctions.Add(a);
        }
        await db.SaveChangesAsync();

        var svc = new AuctionEndBackgroundService(scopeFactory, NullLogger<AuctionEndBackgroundService>.Instance);
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        var endedCount = await db.Auctions.CountAsync(a => a.Status == AuctionStatus.Ended);
        endedCount.Should().Be(100);
    }
}
