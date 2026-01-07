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
    private IContainer _redisContainer;
    private IConnectionMultiplexer _redisConnection;
    private RedisRepository _repository;

    public RedisRepositoryTests()
    {
        // Create Redis container (don't start yet)
        _redisContainer = new ContainerBuilder()
            .WithImage("redis:7")
            .WithPortBinding(6379, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start Redis container
        await _redisContainer.StartAsync();

        // Create Redis connection
        var redisConnectionString = $"localhost:{_redisContainer.GetMappedPublicPort(6379)}";
        _redisConnection = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);

        // Create repository
        var redisConnection = new RedisConnection(redisConnectionString);
        _repository = new RedisRepository(redisConnection);
    }

    public async Task DisposeAsync()
    {
        // Clean up
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
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

        // Act
        var result = await _repository.PlaceBidAsync(bid, TimeSpan.FromDays(7));

        // Assert
        result.Should().BeTrue();

        // Verify data was stored
        var db = _redisConnection.GetDatabase();
        var highestBidKey = $"highest_bid:{bid.AuctionId}";
        var storedData = await db.HashGetAllAsync(highestBidKey);

        storedData.Should().Contain(h => h.Name == "bidId" && h.Value == bid.BidId);
        storedData.Should().Contain(h => h.Name == "bidderId" && h.Value == bid.BidderId);
        storedData.Should().Contain(h => h.Name == "bidderIdHash" && h.Value == bid.BidderIdHash);
        storedData.Should().Contain(h => h.Name == "amount" && h.Value == bid.Amount.Value.ToString());
    }
}