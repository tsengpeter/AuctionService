using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using BiddingService.Core.ValueObjects;
using BiddingService.Infrastructure.BackgroundServices;
using BiddingService.Infrastructure.Data;
using BiddingService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace BiddingService.IntegrationTests.BackgroundServices;

public class RedisSyncWorkerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;
    private IServiceProvider _serviceProvider;
    private readonly Mock<ILogger<RedisSyncWorker>> _loggerMock;

    public RedisSyncWorkerTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("biddingservice_test")
            .WithUsername("postgres")
            .WithPassword("password")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7")
            .Build();

        _loggerMock = new Mock<ILogger<RedisSyncWorker>>();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        var services = new ServiceCollection();
        // Configure services for testing
        services.AddSingleton<ILogger<RedisSyncWorker>>(_loggerMock.Object);
        
        // Add Redis connection
        var redisConnectionString = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}";
        services.AddSingleton<IRedisConnection>(new RedisConnection(redisConnectionString));
        services.AddSingleton<IRedisRepository, RedisRepository>();
        
        // Add PostgreSQL connection and BidRepository for RedisSyncWorker
        var postgresConnectionString = $"Host={_postgresContainer.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};Database=biddingservice_test;Username=postgres;Password=password;SSL Mode=Disable";
        var dbContextOptions = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(postgresConnectionString)
            .Options;
        var dbContext = new BiddingDbContext(dbContextOptions);
        await dbContext.Database.EnsureCreatedAsync();
        services.AddSingleton<IBidRepository>(new BidRepository(dbContext));
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task SyncDeadLetterQueueAsync_WithValidBids_SyncsSuccessfully()
    {
        // Arrange
        var worker = new RedisSyncWorker(_serviceProvider, _loggerMock.Object);

        // Create test bid
        var bid = new Bid(
            1234567890123456789L, // Test ID
            1,
            "test-bidder",
            "hashed-bidder",
            new BidAmount(100.00m),
            DateTime.UtcNow,
            false
        );

        // Add bid to dead letter queue (mock or actual RedisRepository)
        using var scope = _serviceProvider.CreateScope();
        var redisRepo = scope.ServiceProvider.GetRequiredService<IRedisRepository>();
        await redisRepo.AddToDeadLetterQueueAsync(bid);

        // Act
        await worker.SyncDeadLetterQueueWithRetryAsync(CancellationToken.None);

        // Assert
        // Verify bid was synced to database
        var bidRepo = scope.ServiceProvider.GetRequiredService<IBidRepository>();
        var syncedBid = await bidRepo.GetByIdAsync(bid.BidId);
        Assert.NotNull(syncedBid);
        Assert.Equal(bid.BidId, syncedBid.BidId);
        Assert.Equal(bid.AuctionId, syncedBid.AuctionId);
    }

    [Fact]
    public async Task SyncDeadLetterQueueAsync_WithRetryOnFailure_RetriesWithExponentialBackoff()
    {
        // Arrange
        var worker = new RedisSyncWorker(_serviceProvider, _loggerMock.Object);

        // Create test bid and add to dead letter queue
        var bid = new Bid(
            1234567890123456789L, // Test ID
            1,
            "test-bidder",
            "hashed-bidder",
            new BidAmount(100.00m),
            DateTime.UtcNow,
            false
        );

        // Add bid to dead letter queue first
        using var scope = _serviceProvider.CreateScope();
        var redisRepo = scope.ServiceProvider.GetRequiredService<IRedisRepository>();
        await redisRepo.AddToDeadLetterQueueAsync(bid);

        // Mock repository to always fail
        var mockBidRepo = new Mock<IBidRepository>();
        mockBidRepo.Setup(x => x.GetByIdAsync(It.IsAny<long>())).ThrowsAsync(new Exception("Database error"));
        mockBidRepo.Setup(x => x.AddAsync(It.IsAny<Bid>())).ThrowsAsync(new Exception("Database error"));

        var services = new ServiceCollection();
        services.AddSingleton<IBidRepository>(mockBidRepo.Object);
        services.AddSingleton<IRedisRepository>(redisRepo); // Use real Redis repo to get the bid
        services.AddSingleton<IRedisConnection>(new RedisConnection($"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}"));
        services.AddSingleton<ILogger<RedisSyncWorker>>(_loggerMock.Object);
        var failingServiceProvider = services.BuildServiceProvider();

        var failingWorker = new RedisSyncWorker(failingServiceProvider, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => failingWorker.SyncDeadLetterQueueWithRetryAsync(CancellationToken.None));

        // Verify retry delays were logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(3)); // Should retry 3 times with exponential backoff
    }

    [Fact]
    public async Task SyncDeadLetterQueueAsync_EmptyQueue_LogsDebugMessage()
    {
        // Arrange
        var worker = new RedisSyncWorker(_serviceProvider, _loggerMock.Object);

        // Act
        await worker.SyncDeadLetterQueueWithRetryAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("No bids in dead letter queue")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
