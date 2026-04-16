using Auction.Application.Abstractions;
using Auction.Domain.Entities;
using Auction.Domain.Events;
using Auction.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auction.Infrastructure.BackgroundServices;

public class AuctionEndBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AuctionEndBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEndedAuctionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in AuctionEndBackgroundService.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    public async Task ProcessEndedAuctionsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var biddingService = scope.ServiceProvider.GetRequiredService<IBiddingQueryService>();

        var now = DateTimeOffset.UtcNow;

        var expiredAuctions = await db.Auctions
            .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var auction in expiredAuctions)
        {
            try
            {
                var bidInfo = await biddingService.GetHighestBidAsync(auction.Id, cancellationToken);
                auction.End(bidInfo?.WinnerId, bidInfo?.Amount);

                await db.SaveChangesAsync(cancellationToken);

                if (bidInfo is not null)
                {
                    await mediator.Publish(
                        new AuctionWonEvent(auction.Id, bidInfo.WinnerId, bidInfo.Amount, auction.OwnerId),
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ending auction {AuctionId}.", auction.Id);
            }
        }
    }
}
