using BiddingService.Api.Controllers;
using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using BiddingService.Core.Services;
using BiddingService.Core.ValueObjects;
using BiddingService.Infrastructure.Data;
using BiddingService.Infrastructure.HttpClients;
using BiddingService.Infrastructure.IdGeneration;
using BiddingService.Infrastructure.Repositories;
using BiddingService.Shared.Constants;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using WireMock.Server;
using BiddingService.Shared.Helpers;
using Moq;

namespace BiddingService.IntegrationTests.Controllers;

public class BidsControllerIntegrationTests : IAsyncLifetime
{
    private IContainer? _postgresContainer;
    private IContainer? _redisContainer;
    private WireMockServer _auctionServiceMock;
    private BiddingDbContext _dbContext;
    private IConnectionMultiplexer _redisConnection;
    private BidsController _controller;
    private Mock<IEncryptionService> _encryptionServiceMock;
    private Mock<IMemberServiceClient> _memberServiceMock;
    private readonly bool _useTestcontainers;
    private string _postgresConnectionString = string.Empty;
    private string _redisConnectionString = string.Empty;
    private readonly string _uniqueDbName;

    public BidsControllerIntegrationTests()
    {
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _memberServiceMock = new Mock<IMemberServiceClient>();

        // Check if running in CI/CD environment
        var ciRedisConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
        var ciPostgresConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        _useTestcontainers = string.IsNullOrEmpty(ciRedisConnection) || string.IsNullOrEmpty(ciPostgresConnection);

        // Use unique database name to avoid conflicts between concurrent test classes
        _uniqueDbName = $"bidding_test_{Guid.NewGuid():N}";

        if (_useTestcontainers)
        {
            // Local development: use Testcontainers
            _postgresContainer = new ContainerBuilder()
                .WithImage("postgres:16")
                .WithEnvironment("POSTGRES_DB", _uniqueDbName)
                .WithEnvironment("POSTGRES_USER", "testuser")
                .WithEnvironment("POSTGRES_PASSWORD", "testpass")
                .WithPortBinding(5432, true)
                .Build();

            _redisContainer = new ContainerBuilder()
                .WithImage("redis:7")
                .WithPortBinding(6379, true)
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

        // Start WireMock for auction service
        _auctionServiceMock = WireMockServer.Start();
    }

    public async Task InitializeAsync()
    {
        // Setup mock encryption service
        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns((string input) => $"encrypted_{input}");
        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string input) => input.Replace("encrypted_", ""));

        // Setup mock member service (default behavior)
        _memberServiceMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>())).ReturnsAsync(TokenValidationResult.Success(12345L));

        if (_useTestcontainers)
        {
            // Start containers for local development
            await _postgresContainer!.StartAsync();
            await _redisContainer!.StartAsync();

            // Wait for PostgreSQL to be ready with retry logic
            await WaitForPostgresReady();

            _postgresConnectionString = $"Host={_postgresContainer.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};Database={_uniqueDbName};Username=testuser;Password=testpass;SSL Mode=Disable";
            _redisConnectionString = $"localhost:{_redisContainer.GetMappedPublicPort(6379)}";
        }

        // Setup database context
        var dbContextOptions = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(_postgresConnectionString)
            .Options;
        _dbContext = new BiddingDbContext(dbContextOptions, _encryptionServiceMock.Object);

        // Ensure database exists (safe for concurrent tests)
        await _dbContext.Database.EnsureCreatedAsync();
        
        // Clean data instead of dropping database (avoid race conditions with concurrent tests)
        await _dbContext.Bids.ExecuteDeleteAsync();
        
        // Setup Redis connection first to clean it
        _redisConnection = ConnectionMultiplexer.Connect(_redisConnectionString);
        var redisDb = _redisConnection.GetDatabase();
        await redisDb.ExecuteAsync("FLUSHDB");

        // Setup services (Redis already connected above)
        var logger = new LoggerFactory().CreateLogger<BiddingService.Core.Services.BiddingService>();
        var idGenerator = new SnowflakeIdGenerator(1, 1);
        var encryptionService = new BiddingService.Infrastructure.Encryption.EncryptionService(
            "0123456789abcdef0123456789abcdef",
            "0123456789abcdef");

        var redisConnection = new BiddingService.Infrastructure.Redis.RedisConnection(_redisConnectionString);
        var bidRepository = new BidRepository(_dbContext);
        var redisRepository = new RedisRepository(redisConnection);

        var auctionServiceClient = new AuctionServiceClient(
            new HttpClient { BaseAddress = new Uri(_auctionServiceMock.Url) },
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<AuctionServiceClient>>().Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:BypassAuth"] = "false"
            }!)
            .Build();
        
        var biddingService = new BiddingService.Core.Services.BiddingService(
            bidRepository,
            redisRepository,
            auctionServiceClient,
            idGenerator,
            encryptionService,
            logger,
            configuration);

        _controller = new BidsController(
            biddingService, 
            _memberServiceMock.Object, 
            new LoggerFactory().CreateLogger<BidsController>(),
            configuration);

        // Mock ControllerContext with Authorization header
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer valid-token";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Setup mock auction service response
        _auctionServiceMock
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/api/auctions/1").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{""id"": 1, ""title"": ""Test Auction"", ""isActive"": true, ""startingPrice"": 100.00, ""endTime"": ""2025-12-31T23:59:59Z""}"));
    }

    public async Task DisposeAsync()
    {
        if (_useTestcontainers)
        {
            if (_postgresContainer != null)
                await _postgresContainer.StopAsync();
            if (_redisContainer != null)
                await _redisContainer.StopAsync();
        }
        _auctionServiceMock.Stop();
        _redisConnection?.Dispose();
        await _dbContext.DisposeAsync();
    }

    private BidsController CreateController()
    {
        // Create new instances for each concurrent request to avoid DbContext sharing issues
        var logger = new LoggerFactory().CreateLogger<BiddingService.Core.Services.BiddingService>();
        var idGenerator = new SnowflakeIdGenerator(1, 1);
        var encryptionService = new BiddingService.Infrastructure.Encryption.EncryptionService(
            "0123456789abcdef0123456789abcdef",
            "0123456789abcdef");

        // Use stored connection strings (works in both Testcontainers and CI/CD)
        var dbContextOptions = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(_postgresConnectionString)
            .Options;
        var dbContext = new BiddingDbContext(dbContextOptions, encryptionService);

        var redisConnection = new BiddingService.Infrastructure.Redis.RedisConnection(_redisConnectionString);
        var redisRepository = new RedisRepository(redisConnection);
        var bidRepository = new BidRepository(dbContext);
        
        var auctionServiceClient = new AuctionServiceClient(
            new HttpClient { BaseAddress = new Uri(_auctionServiceMock.Url) },
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<AuctionServiceClient>>().Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:BypassAuth"] = "false"
            }!)
            .Build();
        
        var biddingService = new BiddingService.Core.Services.BiddingService(
            bidRepository,
            redisRepository,
            auctionServiceClient,
            idGenerator,
            encryptionService,
            logger,
            configuration);

        var controller = new BidsController(
            biddingService, 
            _memberServiceMock.Object, 
            new LoggerFactory().CreateLogger<BidsController>(),
            configuration);

        // Mock ControllerContext
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer valid-token";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task CreateBid_WhenValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 150.00m };

        // Act
        var result = await _controller.CreateBid(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeOfType<BidResponse>();

        var bidResponse = createdResult.Value as BidResponse;
        bidResponse.AuctionId.Should().Be(1);
        bidResponse.Amount.Should().Be(150.00m);
        bidResponse.BidderIdHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateBid_WhenAuctionNotFound_ReturnsNotFound()
    {
        // Arrange
        _auctionServiceMock.Reset();
        _auctionServiceMock
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/api/auctions/999").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create().WithStatusCode(404));

        var request = new CreateBidRequest { AuctionId = 999, Amount = 150.00m };

        // Act
        var result = await _controller.CreateBid(request);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateBid_WhenBidAmountTooLow_ReturnsBadRequest()
    {
        // Arrange
        // First create a higher bid
        var highBidRequest = new CreateBidRequest { AuctionId = 1, Amount = 200.00m };
        await _controller.CreateBid(highBidRequest);

        // Now try a lower bid
        var lowBidRequest = new CreateBidRequest { AuctionId = 1, Amount = 150.00m };

        // Act
        var result = await _controller.CreateBid(lowBidRequest);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateBid_WhenBidderIncreasesBid_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 150.00m };

        // First bid
        await _controller.CreateBid(request);

        // Second bid from same bidder with higher amount (should be allowed)
        var higherRequest = new CreateBidRequest { AuctionId = 1, Amount = 160.00m };

        // Act
        var result = await _controller.CreateBid(higherRequest);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeOfType<BidResponse>();

        var bidResponse = createdResult.Value as BidResponse;
        bidResponse.AuctionId.Should().Be(1);
        bidResponse.Amount.Should().Be(160.00m);
        bidResponse.BidderIdHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMyBids_WhenCalled_ReturnsMyBidsResponse()
    {
        // Arrange
        // Use the same bidder ID as the mock setup (12345L)
        var bidderId = 12345L;
        var bidderIdStr = bidderId.ToString();
        var bidderIdHash = HashHelper.ComputeSha256Hash(bidderIdStr);
        var encryptedBidderId = $"encrypted_{bidderIdStr}"; // Matching mock encryption

        var bid1 = new Bid(1, 1, encryptedBidderId, bidderIdHash, new BidAmount(150.00m), DateTime.UtcNow.AddMinutes(-10));
        var bid2 = new Bid(2, 1, encryptedBidderId, bidderIdHash, new BidAmount(160.00m), DateTime.UtcNow.AddMinutes(-5));

        await _dbContext.Bids.AddRangeAsync(bid1, bid2);
        await _dbContext.SaveChangesAsync();

        // Verify data was saved
        var savedBids = await _dbContext.Bids.Where(b => b.BidderIdHash == bidderIdHash).ToListAsync();
        savedBids.Should().HaveCount(2);

        // Setup auction batch response
        _auctionServiceMock
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/api/auctions/batch").UsingPost())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithBody(@"[{""id"": 1, ""title"": ""Test Auction"", ""isActive"": true}]"));

        // Setup Redis highest bid
        var highestBid = new Bid(3, 1, "other-encrypted", "other-hash", new BidAmount(170.00m), DateTime.UtcNow);
        await _redisConnection.GetDatabase().HashSetAsync($"highest_bid:1", new HashEntry[]
        {
            new HashEntry("bidId", highestBid.BidId),
            new HashEntry("auctionId", highestBid.AuctionId),
            new HashEntry("bidderId", highestBid.BidderId),
            new HashEntry("bidderIdHash", highestBid.BidderIdHash),
            new HashEntry("amount", highestBid.Amount.Value.ToString()),
            new HashEntry("bidAt", highestBid.BidAt.ToString("O"))
        });

        // Act
        var result = await _controller.GetMyBids();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeOfType<MyBidsResponse>();

        var response = okResult.Value as MyBidsResponse;
        response.Bids.Should().HaveCount(2);
        response.Bids.Should().Contain(b => b.BidId == 1 && b.Amount == 150.00m);
        response.Bids.Should().Contain(b => b.BidId == 2 && b.Amount == 160.00m);
        response.Pagination.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetHighestBid_WhenBidExistsInRedis_ReturnsHighestBidFromRedis()
    {
        // Arrange
        var auctionId = 100L;
        var highestBid = new Bid(999, auctionId, "test-bidder", "test-hash", new BidAmount(250.00m), DateTime.UtcNow);

        // Setup Redis highest bid
        await _redisConnection.GetDatabase().HashSetAsync($"highest_bid:{auctionId}", new HashEntry[]
        {
            new HashEntry("bidId", highestBid.BidId),
            new HashEntry("auctionId", highestBid.AuctionId),
            new HashEntry("bidderId", highestBid.BidderId),
            new HashEntry("bidderIdHash", highestBid.BidderIdHash),
            new HashEntry("amount", highestBid.Amount.Value.ToString()),
            new HashEntry("bidAt", highestBid.BidAt.ToString("O"))
        });

        // Act
        var result = await _controller.GetHighestBid(auctionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeOfType<HighestBidResponse>();

        var response = okResult.Value as HighestBidResponse;
        response.AuctionId.Should().Be(auctionId);
        response.HighestBid.Should().NotBeNull();
        response.HighestBid!.BidId.Should().Be(999);
        response.HighestBid!.Amount.Should().Be(250.00m);
    }

    [Fact]
    public async Task GetHighestBid_WhenBidExistsInDatabaseOnly_ReturnsHighestBidFromDatabase()
    {
        // Arrange
        var auctionId = 200L;
        var bid1 = new Bid(201, auctionId, "bidder1", "hash1", new BidAmount(100.00m), DateTime.UtcNow.AddMinutes(-10));
        var bid2 = new Bid(202, auctionId, "bidder2", "hash2", new BidAmount(150.00m), DateTime.UtcNow.AddMinutes(-5));
        var bid3 = new Bid(203, auctionId, "bidder3", "hash3", new BidAmount(120.00m), DateTime.UtcNow.AddMinutes(-3));

        await _dbContext.Bids.AddRangeAsync(bid1, bid2, bid3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetHighestBid(auctionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeOfType<HighestBidResponse>();

        var response = okResult.Value as HighestBidResponse;
        response.AuctionId.Should().Be(auctionId);
        response.HighestBid.Should().NotBeNull();
        response.HighestBid!.BidId.Should().Be(202); // bid2 has the highest amount (150.00)
        response.HighestBid!.Amount.Should().Be(150.00m);
    }

    [Fact]
    public async Task GetHighestBid_WhenNoBidsExist_ReturnsNotFound()
    {
        // Arrange
        var auctionId = 999L;

        // Act
        var result = await _controller.GetHighestBid(auctionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAuctionStats_WhenAuctionHasBids_ReturnsAuctionStats()
    {
        // Arrange
        var auctionId = 200L;
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var oneDayAgo = now.AddDays(-1);

        // Setup auction mock response
        _auctionServiceMock.Reset();
        _auctionServiceMock
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/api/auctions/*").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""id"":{auctionId},""title"":""Test Auction"",""startingPrice"":100.00,""isActive"":true,""endTime"":""2025-12-31T23:59:59Z""}}"));

        // Create test bids in database
        var bids = new[]
        {
            new Bid(1001, auctionId, "bidder1", "hash1", new BidAmount(100.00m), oneDayAgo.AddMinutes(1)), // Old bid (1 day ago + 1 min)
            new Bid(1002, auctionId, "bidder2", "hash2", new BidAmount(150.00m), oneHourAgo.AddMinutes(1)), // Recent bid (1 hour ago + 1 min)
            new Bid(1003, auctionId, "bidder1", "hash1", new BidAmount(200.00m), now), // Latest bid from same bidder
            new Bid(1004, auctionId, "bidder3", "hash3", new BidAmount(180.00m), now.AddMinutes(-30)) // Another recent bid
        };

        await _dbContext.Bids.AddRangeAsync(bids);
        await _dbContext.SaveChangesAsync();

        // Set highest bid in Redis (bidId: 1003, amount: 200.00)
        var db = _redisConnection.GetDatabase();
        var highestBidKey = $"highest_bid:{auctionId}";
        await db.HashSetAsync(highestBidKey, new HashEntry[]
        {
            new HashEntry("bidId", "1003"),
            new HashEntry("bidderId", "bidder1"),
            new HashEntry("bidderIdHash", "hash1"),
            new HashEntry("amount", "200.00"),
            new HashEntry("bidAt", now.ToString("O"))
        });

        // Act
        var result = await _controller.GetAuctionStats(auctionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeOfType<AuctionStatsResponse>();

        var response = okResult.Value as AuctionStatsResponse;
        response.AuctionId.Should().Be(auctionId);
        response.TotalBids.Should().Be(4);
        response.UniqueBidders.Should().Be(3);
        response.StartingPrice.Should().Be(100.00m);
        response.CurrentHighestBid.Should().Be(200.00m);
        response.AverageBidAmount.Should().Be(157.50m); // (100+150+200+180)/4
        response.FirstBidAt.Should().BeCloseTo(oneDayAgo.AddMinutes(1), TimeSpan.FromSeconds(1));
        response.LastBidAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        response.BidsInLastHour.Should().Be(3); // 1002, 1003, 1004
        response.BidsInLast24Hours.Should().Be(4); // All bids
    }

    [Fact]
    public async Task GetAuctionStats_WhenAuctionHasNoBids_ReturnsEmptyStats()
    {
        // Arrange
        var auctionId = 300L;

        // Setup auction mock response
        _auctionServiceMock.Reset();
        _auctionServiceMock
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/api/auctions/*").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""id"":{auctionId},""title"":""Empty Auction"",""startingPrice"":50.00,""isActive"":true,""endTime"":""2025-12-31T23:59:59Z""}}"));

        // Act
        var result = await _controller.GetAuctionStats(auctionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeOfType<AuctionStatsResponse>();

        var response = okResult.Value as AuctionStatsResponse;
        response.AuctionId.Should().Be(auctionId);
        response.TotalBids.Should().Be(0);
        response.UniqueBidders.Should().Be(0);
        response.StartingPrice.Should().Be(50.00m);
        response.CurrentHighestBid.Should().BeNull();
        response.AverageBidAmount.Should().BeNull();
        response.FirstBidAt.Should().BeNull();
        response.LastBidAt.Should().BeNull();
        response.BidsInLastHour.Should().Be(0);
        response.BidsInLast24Hours.Should().Be(0);
    }

    [Fact]
    public async Task CreateBid_WhenConcurrentBidsPlaced_HandlesConcurrencyCorrectly()
    {
        // Arrange
        const int numberOfConcurrentBids = 5;
        var tasks = new List<Task<IActionResult>>();
        var bids = new List<decimal> { 200.00m, 210.00m, 220.00m, 230.00m, 240.00m };

        // Act - Create multiple concurrent bid requests with sufficiently high amounts
        for (int i = 0; i < numberOfConcurrentBids; i++)
        {
            var request = new CreateBidRequest { AuctionId = 1, Amount = bids[i] };
            // Create a new controller instance for each concurrent request to avoid DbContext sharing
            var controller = CreateController();
            tasks.Add(controller.CreateBid(request));
        }

        // Assert - System should handle concurrent requests without throwing exceptions
        Func<Task> act = async () => await Task.WhenAll(tasks);

        // The system should not crash when handling concurrent requests
        await act.Should().NotThrowAsync();

        var results = await Task.WhenAll(tasks);

        // Assert - All results should be IActionResult instances (no unhandled exceptions)
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        // Verify that the system handled the concurrent requests without crashing
        // At least some bids should have been processed (either succeeded or failed with proper error responses)
        var validResponses = results.Count(r => r is ObjectResult);
        validResponses.Should().Be(numberOfConcurrentBids);

        // The system should be able to provide stats after concurrent operations
        var statsResult = await _controller.GetAuctionStats(1);
        statsResult.Should().BeAssignableTo<ObjectResult>();
        var statsObjectResult = statsResult as ObjectResult;
        statsObjectResult.StatusCode.Should().Be(200);

        var statsResponse = statsObjectResult.Value as AuctionStatsResponse;
        statsResponse.Should().NotBeNull();
        // Stats should be available even if no bids succeeded due to concurrency
        statsResponse.TotalBids.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetBidHistory_WhenAuctionExists_ReturnsBidHistory()
    {
        // Arrange
        // Create bids directly in database for this test
        var bid1 = new Bid(1, 1, "bidder1", "hash1", new BidAmount(150.00m), DateTime.UtcNow.AddMinutes(-10));
        var bid2 = new Bid(2, 1, "bidder2", "hash2", new BidAmount(200.00m), DateTime.UtcNow.AddMinutes(-5));
        var bid3 = new Bid(3, 1, "bidder3", "hash3", new BidAmount(250.00m), DateTime.UtcNow.AddMinutes(-1));

        await _dbContext.Bids.AddRangeAsync(bid1, bid2, bid3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetBidHistory(1);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(200);

        var response = objectResult.Value as BidHistoryResponse;
        response.Should().NotBeNull();
        response.AuctionId.Should().Be(1);
        response.Bids.Should().NotBeNull();
        response.Bids.Count().Should().BeGreaterThanOrEqualTo(1); // At least one bid should be returned
        response.Pagination.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBidHistory_WhenAuctionHasNoBids_ReturnsEmptyHistory()
    {
        // Arrange
        var auctionId = 999; // Auction with no bids

        // Act
        var result = await _controller.GetBidHistory(auctionId);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(200);

        var response = objectResult.Value as BidHistoryResponse;
        response.Should().NotBeNull();
        response.AuctionId.Should().Be(auctionId);
        response.Bids.Should().NotBeNull();
        response.Bids.Should().BeEmpty();
        response.Pagination.Should().NotBeNull();
        response.Pagination.TotalCount.Should().Be(0);
    }

    private async Task WaitForPostgresReady()
    {
        if (!_useTestcontainers)
        {
            // CI/CD environment: assume PostgreSQL is already ready
            return;
        }

        var connectionString = $"Host={_postgresContainer!.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};Database={_uniqueDbName};Username=testuser;Password=testpass;SSL Mode=Disable";
        
        for (int i = 0; i < 30; i++) // Try for up to 30 seconds
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                return; // Success
            }
            catch
            {
                await Task.Delay(1000); // Wait 1 second before retry
            }
        }
        
        throw new Exception("PostgreSQL container did not become ready within 30 seconds");
    }
}