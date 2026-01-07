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
    private readonly PostgreSqlContainer? _postgresContainer;
    private readonly RedisContainer? _redisContainer;
    private IServiceProvider _serviceProvider;
    private readonly Mock<ILogger<RedisSyncWorker>> _loggerMock;
    private readonly bool _useTestcontainers;
    private string _redisConnectionString = string.Empty;
    private string _postgresConnectionString = string.Empty;
    private readonly string _uniqueDbName;

    public RedisSyncWorkerTests()
    {
        _loggerMock = new Mock<ILogger<RedisSyncWorker>>();

        // Check if running in CI/CD environment
        var ciRedisConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
        var ciPostgresConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        _useTestcontainers = string.IsNullOrEmpty(ciRedisConnection) || string.IsNullOrEmpty(ciPostgresConnection);

        // Use unique database name to avoid conflicts between concurrent test classes
        _uniqueDbName = $"biddingservice_test_{Guid.NewGuid():N}";

        if (_useTestcontainers)
        {
            // Local development: use Testcontainers
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase(_uniqueDbName)
                .WithUsername("postgres")
                .WithPassword("password")
                .Build();

            _redisContainer = new RedisBuilder()
                .WithImage("redis:7")
                .Build();
        }
        else
        {
            // CI/CD: use pre-configured services with unique database name
            var baseConnection = ciPostgresConnection!.Split(';');
            var modifiedConnection = new List<string>();
            foreach (var part in baseConnection)
            {
                if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                {
                    modifiedConnection.Add($"Database={_uniqueDbName}");
                }
                else
                {
                    modifiedConnection.Add(part);
                }
            }
            _postgresConnectionString = string.Join(";", modifiedConnection);
            _redisConnectionString = ciRedisConnection!;
        }
    }

    public async Task InitializeAsync()
    {
        if (_useTestcontainers)
        {
            await _postgresContainer!.StartAsync();
            await _redisContainer!.StartAsync();
            _redisConnectionString = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}";
            _postgresConnectionString = $"Host={_postgresContainer.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};Database={_uniqueDbName};Username=postgres;Password=password;SSL Mode=Disable";
        }

        var services = new ServiceCollection();
        // Configure services for testing
        services.AddSingleton<ILogger<RedisSyncWorker>>(_loggerMock.Object);
        var encryptionServiceMock = new Mock<IEncryptionService>();
        encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns((string input) => $"encrypted_{input}");
        encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string input) => input.Replace("encrypted_", ""));
        services.AddSingleton<IEncryptionService>(encryptionServiceMock.Object);
        
        // Setup Redis connection and clean data for test isolation
        var redisConnection = new RedisConnection(_redisConnectionString);
        var connectionMultiplexer = (StackExchange.Redis.ConnectionMultiplexer)redisConnection.Connection;
        var redisDb = connectionMultiplexer.GetDatabase();
        await redisDb.ExecuteAsync("FLUSHDB");
        services.AddSingleton<IRedisConnection>(redisConnection);
        services.AddSingleton<IRedisRepository, RedisRepository>();
        
        // Add PostgreSQL connection and BidRepository for RedisSyncWorker
        var dbContextOptions = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(_postgresConnectionString)
            .Options;
        var dbContext = new BiddingDbContext(dbContextOptions, encryptionServiceMock.Object);
        
        // Ensure database exists (safe for concurrent tests)
        await dbContext.Database.EnsureCreatedAsync();
        
        // Clean data instead of dropping database (avoid race conditions with concurrent tests)
        await dbContext.Bids.ExecuteDeleteAsync();
        
        services.AddSingleton<IBidRepository>(new BidRepository(dbContext));
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_useTestcontainers)
        {
            if (_postgresContainer != null)
                await _postgresContainer.DisposeAsync();
            if (_redisContainer != null)
                await _redisContainer.DisposeAsync();
        }
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
        await redisRepo.AddToDeadLetterQueueAsync(bid, "Test sync failure");

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
        await redisRepo.AddToDeadLetterQueueAsync(bid, "Test retry failure");

        // Mock repository to always fail
        var mockBidRepo = new Mock<IBidRepository>();
        mockBidRepo.Setup(x => x.GetByIdAsync(It.IsAny<long>())).ThrowsAsync(new Exception("Database error"));
        mockBidRepo.Setup(x => x.AddAsync(It.IsAny<Bid>())).ThrowsAsync(new Exception("Database error"));

        var services = new ServiceCollection();
        services.AddSingleton<IBidRepository>(mockBidRepo.Object);
        services.AddSingleton<IRedisRepository>(redisRepo); // Use real Redis repo to get the bid
        services.AddSingleton<IRedisConnection>(new RedisConnection(_redisConnectionString)); // Use stored connection string (works in both local and CI/CD)
        services.AddSingleton<ILogger<RedisSyncWorker>>(_loggerMock.Object);
        var failingServiceProvider = services.BuildServiceProvider();

        var failingWorker = new RedisSyncWorker(failingServiceProvider, _loggerMock.Object);

        // Act - Run DLQ processing multiple times to trigger retries
        await failingWorker.SyncDeadLetterQueueWithRetryAsync(CancellationToken.None);
        await failingWorker.SyncDeadLetterQueueWithRetryAsync(CancellationToken.None);
        await failingWorker.SyncDeadLetterQueueWithRetryAsync(CancellationToken.None);
        
        // After max retries, the bid should be removed from DLQ
        await failingWorker.SyncDeadLetterQueueWithRetryAsync(CancellationToken.None);

        // Assert - Verify retry logic was executed
        var dlqBids = await redisRepo.GetDeadLetterBidsAsync(100);
        Assert.Empty(dlqBids); // bid should be removed after exceeding max retries

        // Verify error was logged (1 DLQ sync error + 1 retry wrapper error)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2));
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
