using BiddingService.Core.Interfaces;
using BiddingService.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.Redis;
using Xunit;

namespace BiddingService.IntegrationTests.BackgroundServices;

public class RedisHealthCheckServiceTests : IAsyncLifetime
{
    private readonly RedisContainer? _redisContainer;
    private RedisHealthCheckService _healthCheckService = null!;
    private readonly bool _useTestcontainers;
    private string _redisConnectionString = string.Empty;

    public RedisHealthCheckServiceTests()
    {
        // Check if running in CI/CD environment
        var ciRedisConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
        _useTestcontainers = string.IsNullOrEmpty(ciRedisConnection);

        if (_useTestcontainers)
        {
            // Local development: use Testcontainers
            _redisContainer = new RedisBuilder()
                .WithImage("redis:7")
                .Build();
        }
        else
        {
            // CI/CD: use pre-configured Redis service
            _redisConnectionString = ciRedisConnection!;
        }
    }

    public async Task InitializeAsync()
    {
        if (_useTestcontainers && _redisContainer != null)
        {
            await _redisContainer.StartAsync();
            _redisConnectionString = _redisContainer.GetConnectionString();
        }

        // Create Redis connection
        var redisConnection = new RedisConnection(_redisConnectionString);
        _healthCheckService = new RedisHealthCheckService(redisConnection);
    }

    public async Task DisposeAsync()
    {
        if (_useTestcontainers && _redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
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
        // Skip in CI/CD environment where we can't control Redis lifecycle
        if (!_useTestcontainers)
        {
            return;
        }

        // Arrange - Stop Redis container to simulate failure
        await _redisContainer!.StopAsync();

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
