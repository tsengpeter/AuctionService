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
    private readonly TimeSpan[] _retryDelays = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

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
                await SyncPendingBidsAsync(stoppingToken);
                await SyncDeadLetterQueueWithRetryInternalAsync(stoppingToken);
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("RedisSyncWorker stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RedisSyncWorker");
                try
                {
                    await Task.Delay(_syncInterval, stoppingToken);
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("RedisSyncWorker stopping");
                    break;
                }
            }
        }

        _logger.LogInformation("RedisSyncWorker stopped");
    }

    private async Task SyncPendingBidsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var redisRepository = scope.ServiceProvider.GetRequiredService<IRedisRepository>();
        var bidRepository = scope.ServiceProvider.GetRequiredService<IBidRepository>();

        var pendingMembers = await redisRepository.GetPendingBidMembersAsync(100);
        if (!pendingMembers.Any()) return;

        foreach (var member in pendingMembers)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // member format: "bidId:auctionId"
            var parts = member.Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var bidId))
            {
                _logger.LogError("Invalid pending bid member format: {Member}", member);
                await redisRepository.RemovePendingBidMemberAsync(member);
                continue;
            }

            try
            {
                var bid = await redisRepository.GetBidInfoAsync(bidId);
                if (bid == null)
                {
                    _logger.LogWarning("Bid info not found for bid {BidId} (member: {Member})", bidId, member);
                    // Decide whether to remove the pending member. If info is gone, we can't sync.
                    await redisRepository.RemovePendingBidMemberAsync(member);
                    continue;
                }

                // Check existence
                var existing = await bidRepository.GetByIdAsync(bid.BidId);
                if (existing == null)
                {
                    await bidRepository.AddAsync(bid);
                    _logger.LogDebug("Synced bid {BidId}", bid.BidId);
                }

                // Cleanup
                await redisRepository.RemovePendingBidMemberAsync(member);
                await redisRepository.RemoveBidInfoAsync(bid.BidId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync bid {BidId}", bidId);
                // We leave it in pending_bids to retry later
                // Optionally move to Dead Letter if retries exceed limit (needs tracking)
            }
        }
    }

    private async Task SyncDeadLetterQueueWithRetryInternalAsync(CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var maxRetries = _retryDelays.Length;

        while (retryCount <= maxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await SyncDeadLetterQueueAsync(cancellationToken);
                return; // Success, exit retry loop
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount <= maxRetries)
                {
                    var delay = _retryDelays[retryCount - 1];
                    _logger.LogWarning(ex, "Sync failed, retrying in {Delay}s (attempt {RetryCount}/{MaxRetries})",
                        delay.TotalSeconds, retryCount, maxRetries);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, "Sync failed after {MaxRetries} retries, giving up", maxRetries);
                    throw;
                }
            }
        }
    }

    // Public method for testing purposes
    public async Task SyncDeadLetterQueueWithRetryAsync(CancellationToken cancellationToken)
    {
        await SyncDeadLetterQueueWithRetryInternalAsync(cancellationToken);
    }

    private async Task SyncDeadLetterQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var redisRepository = scope.ServiceProvider.GetRequiredService<IRedisRepository>();
        var bidRepository = scope.ServiceProvider.GetRequiredService<IBidRepository>();

        var deadLetterBids = await redisRepository.GetDeadLetterBidsAsync(100);

        if (!deadLetterBids.Any())
        {
            _logger.LogDebug("No bids in dead letter queue");
            return;
        }

        _logger.LogInformation("Processing {Count} bids from dead letter queue", deadLetterBids.Count());

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
                    _logger.LogDebug("Synced bid {BidId} for auction {AuctionId}", bid.BidId, bid.AuctionId);
                }
                else
                {
                    _logger.LogDebug("Bid {BidId} already exists, skipping", bid.BidId);
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

        _logger.LogInformation("Sync completed: {SyncedCount} bids synced, {FailedCount} failed, {RemainingCount} remaining in queue",
            syncedCount, failedCount, deadLetterBids.Count() - syncedCount - failedCount);

        // If all bids failed to sync, throw an exception to trigger retry
        if (syncedCount == 0 && failedCount > 0)
        {
            throw new Exception($"Failed to sync any bids from dead letter queue. {failedCount} bids failed.");
        }
    }
}
