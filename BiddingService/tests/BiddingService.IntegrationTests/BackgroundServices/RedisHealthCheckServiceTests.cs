using BiddingService.Core.Interfaces;
using BiddingService.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.Redis;
using Xunit;

namespace BiddingService.IntegrationTests.BackgroundServices;

public class RedisHealthCheckServiceTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private RedisHealthCheckService _healthCheckService = null!;

    public RedisHealthCheckServiceTests()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();

        // Create mock Redis connection
        var redisConnection = new RedisConnection(_redisContainer.GetConnectionString());
        _healthCheckService = new RedisHealthCheckService(redisConnection);
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task CheckHealthAsync_RedisAvailable_ReturnsHealthy()
    {
        // Act
        var result = await _healthCheckService.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Redis ping:", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_RedisUnavailable_ReturnsUnhealthy()
    {
        // Arrange - Stop Redis container to simulate failure
        await _redisContainer.StopAsync();

        // Act
        var result = await _healthCheckService.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis connection failed", result.Description);
        Assert.NotNull(result.Exception);
    }
}

// Mock Redis connection for testing
internal class RedisConnection : IRedisConnection
{
    private readonly string _connectionString;

    public RedisConnection(string connectionString)
    {
        _connectionString = connectionString;
    }

    public object Connection => StackExchange.Redis.ConnectionMultiplexer.Connect(_connectionString);
}
