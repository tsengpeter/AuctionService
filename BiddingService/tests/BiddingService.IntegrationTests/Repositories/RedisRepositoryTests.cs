using BiddingService.Core.Entities;
using BiddingService.Core.ValueObjects;
using BiddingService.Infrastructure.Redis;
using BiddingService.Infrastructure.Repositories;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace BiddingService.IntegrationTests.Repositories;

public class RedisRepositoryTests : IAsyncLifetime
{
    private IContainer? _redisContainer;
    private IConnectionMultiplexer _redisConnection = null!;
    private RedisRepository _repository = null!;
    private readonly bool _useTestcontainers;
    private string _redisConnectionString;

    public RedisRepositoryTests()
    {
        // Check if running in CI/CD environment (GitHub Actions provides Redis service)
        var ciRedisConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
        _useTestcontainers = string.IsNullOrEmpty(ciRedisConnection);

        if (_useTestcontainers)
        {
            // Local development: use Testcontainers
            _redisContainer = new ContainerBuilder()
                .WithImage("redis:7")
                .WithPortBinding(6379, true)
                .Build();
            _redisConnectionString = string.Empty; // Will be set after container starts
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
            // Start Redis container for local development
            await _redisContainer.StartAsync();
            _redisConnectionString = $"localhost:{_redisContainer.GetMappedPublicPort(6379)}";
        }

        // Create Redis connection
        _redisConnection = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);

        // Create repository
        var redisConnection = new RedisConnection(_redisConnectionString);
        _repository = new RedisRepository(redisConnection);
    }

    public async Task DisposeAsync()
    {
        // Clean up Redis database before disposing
        if (_redisConnection != null)
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var endpoints = _redisConnection.GetEndPoints();
                var server = _redisConnection.GetServer(endpoints.First());
                await server.FlushDatabaseAsync();
            }
            catch (Exception)
            {
                // Ignore cleanup errors in DisposeAsync
            }
        }

        // Clean up containers
        if (_useTestcontainers && _redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
        _redisConnection?.Dispose();
    }

    [Fact]
    public async Task GetHighestBidAsync_WhenHighestBidExists_ReturnsHighestBid()
    {
        // Arrange
        var auctionId = 100L;
        var expectedBid = new Bid(999, auctionId, "test-bidder", "test-hash", new BidAmount(250.00m), DateTime.UtcNow);

        // Setup Redis data
        var db = _redisConnection.GetDatabase();
        var highestBidKey = $"highest_bid:{auctionId}";
        await db.HashSetAsync(highestBidKey, new HashEntry[]
        {
            new HashEntry("bidId", expectedBid.BidId),
            new HashEntry("bidderId", expectedBid.BidderId),
            new HashEntry("bidderIdHash", expectedBid.BidderIdHash),
            new HashEntry("amount", expectedBid.Amount.Value.ToString()),
            new HashEntry("bidAt", expectedBid.BidAt.ToString("O"))
        });

        // Act
        var result = await _repository.GetHighestBidAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result!.BidId.Should().Be(expectedBid.BidId);
        result.AuctionId.Should().Be(expectedBid.AuctionId);
        result.BidderId.Should().Be(expectedBid.BidderId);
        result.BidderIdHash.Should().Be(expectedBid.BidderIdHash);
        result.Amount.Value.Should().Be(expectedBid.Amount.Value);
        result.BidAt.Should().BeCloseTo(expectedBid.BidAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetHighestBidAsync_WhenHighestBidDoesNotExist_ReturnsNull()
    {
        // Arrange
        var auctionId = 999L;

        // Act
        var result = await _repository.GetHighestBidAsync(auctionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PlaceBidAsync_WhenCalled_UpdatesHighestBidInRedis()
    {
        // Arrange
        var bid = new Bid(123, 456, "bidder", "hash", new BidAmount(100.00m), DateTime.UtcNow);

        // Clean up Redis before test to ensure no stale data
        var db = _redisConnection.GetDatabase();
        var highestBidKey = $"highest_bid:{bid.AuctionId}";
        await db.KeyDeleteAsync(highestBidKey);

        // Act
        var result = await _repository.PlaceBidAsync(bid, TimeSpan.FromDays(7));

        // Assert
        result.Should().BeTrue();

        // Verify data was stored
        var storedData = await db.HashGetAllAsync(highestBidKey);

        storedData.Should().Contain(h => h.Name == "bidId" && h.Value == bid.BidId);
        storedData.Should().Contain(h => h.Name == "bidderId" && h.Value == bid.BidderId);
        storedData.Should().Contain(h => h.Name == "bidderIdHash" && h.Value == bid.BidderIdHash);
        storedData.Should().Contain(h => h.Name == "amount" && h.Value == bid.Amount.Value.ToString());
    }
}