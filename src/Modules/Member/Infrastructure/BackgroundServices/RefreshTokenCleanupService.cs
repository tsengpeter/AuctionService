using Member.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Member.Infrastructure.BackgroundServices;

public sealed class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MemberDbContext>();

            var now = DateTimeOffset.UtcNow;
            var deleted = await db.RefreshTokens
                .Where(t => t.IsRevoked || t.ExpiresAt <= now)
                .ExecuteDeleteAsync(cancellationToken);

            logger.LogInformation("RefreshToken cleanup complete. Deleted {Count} expired/revoked tokens.", deleted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RefreshToken cleanup failed. Will retry in {Interval}.", Interval);
        }
    }
}
