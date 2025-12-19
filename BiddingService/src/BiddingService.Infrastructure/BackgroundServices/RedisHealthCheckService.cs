using BiddingService.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace BiddingService.Infrastructure.BackgroundServices;

public class RedisHealthCheckService : IHealthCheck
{
    private readonly IRedisConnection _redisConnection;

    public RedisHealthCheckService(IRedisConnection redisConnection)
    {
        _redisConnection = redisConnection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = (IConnectionMultiplexer)_redisConnection.Connection;
            var db = connection.GetDatabase();
            var pong = await db.PingAsync();

            return HealthCheckResult.Healthy($"Redis ping: {pong.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}