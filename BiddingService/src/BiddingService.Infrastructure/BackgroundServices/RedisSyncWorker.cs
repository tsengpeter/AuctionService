using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BiddingService.Infrastructure.BackgroundServices;

public class RedisSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RedisSyncWorker> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(1);

    public RedisSyncWorker(
        IServiceProvider serviceProvider,
        ILogger<RedisSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RedisSyncWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncDeadLetterQueueAsync(stoppingToken);
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RedisSyncWorker");
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        _logger.LogInformation("RedisSyncWorker stopped");
    }

    private async Task SyncDeadLetterQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var redisRepository = scope.ServiceProvider.GetRequiredService<IRedisRepository>();
        var bidRepository = scope.ServiceProvider.GetRequiredService<IBidRepository>();

        var deadLetterBids = await redisRepository.GetDeadLetterBidsAsync(100);

        if (!deadLetterBids.Any())
            return;

        var syncedCount = 0;
        var failedCount = 0;

        foreach (var bid in deadLetterBids)
        {
            try
            {
                // Check if already exists
                var existing = await bidRepository.GetByIdAsync(bid.BidId);
                if (existing == null)
                {
                    await bidRepository.AddAsync(bid);
                    syncedCount++;
                }

                // Remove from dead letter queue
                await redisRepository.RemoveDeadLetterBidsAsync(new[] { bid.BidId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync bid {BidId}", bid.BidId);
                failedCount++;
            }
        }

        _logger.LogInformation("Synced {SyncedCount} bids, {FailedCount} failed", syncedCount, failedCount);
    }
}