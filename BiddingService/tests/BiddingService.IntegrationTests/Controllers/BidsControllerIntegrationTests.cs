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
using StackExchange.Redis;
using System.Net;
using WireMock.Server;
using BiddingService.Shared.Helpers;

namespace BiddingService.IntegrationTests.Controllers;

public class BidsControllerIntegrationTests : IAsyncLifetime
{
    private IContainer _postgresContainer;
    private IContainer _redisContainer;
    private WireMockServer _auctionServiceMock;
    private BiddingDbContext _dbContext;
    private IConnectionMultiplexer _redisConnection;
    private BidsController _controller;

    public BidsControllerIntegrationTests()
    {
        // Create containers (don't start yet)
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:14")
            .WithEnvironment("POSTGRES_DB", "bidding_test")
            .WithEnvironment("POSTGRES_USER", "testuser")
            .WithEnvironment("POSTGRES_PASSWORD", "testpass")
            .WithPortBinding(5432, true)
            .Build();

        _redisContainer = new ContainerBuilder()
            .WithImage("redis:7")
            .WithPortBinding(6379, true)
            .Build();

        // Start WireMock for auction service
        _auctionServiceMock = WireMockServer.Start();
    }

    public async Task InitializeAsync()
    {
        // Start containers
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Wait for PostgreSQL to be ready
        await Task.Delay(5000); // Wait 5 seconds for PostgreSQL to initialize

        // Setup database context
        var postgresConnectionString = $"Host=localhost;Port={_postgresContainer.GetMappedPublicPort(5432)};Database=bidding_test;Username=testuser;Password=testpass;SSL Mode=Disable";
        var dbContextOptions = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(postgresConnectionString)
            .Options;
        _dbContext = new BiddingDbContext(dbContextOptions);

        // Ensure database is created
        await _dbContext.Database.EnsureCreatedAsync();

        // Setup Redis connection
        var redisConnectionString = $"localhost:{_redisContainer.GetMappedPublicPort(6379)}";
        _redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

        // Setup services
        var logger = new LoggerFactory().CreateLogger<BiddingService.Core.Services.BiddingService>();
        var idGenerator = new SnowflakeIdGenerator(1, 1);
        var encryptionService = new BiddingService.Infrastructure.Encryption.EncryptionService(
            "0123456789abcdef0123456789abcdef",
            "0123456789abcdef");

        var redisConnection = new BiddingService.Infrastructure.Redis.RedisConnection(redisConnectionString);
        var bidRepository = new BidRepository(_dbContext);
        var redisRepository = new RedisRepository(redisConnection);

        var auctionServiceClient = new AuctionServiceClient(
            new HttpClient { BaseAddress = new Uri(_auctionServiceMock.Url) },
            new MemoryCache(new MemoryCacheOptions()));

        var biddingService = new BiddingService.Core.Services.BiddingService(
            bidRepository,
            redisRepository,
            auctionServiceClient,
            idGenerator,
            encryptionService,
            logger);

        _controller = new BidsController(biddingService, new LoggerFactory().CreateLogger<BidsController>());

        // Setup mock auction service response
        _auctionServiceMock
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/api/auctions/1").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{""id"": 1, ""title"": ""Test Auction"", ""isActive"": true, ""startingPrice"": 100.00, ""endTime"": ""2025-12-31T23:59:59Z""}"));
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
        _auctionServiceMock.Stop();
        _redisConnection?.Dispose();
        await _dbContext.DisposeAsync();
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
        result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateBid_WhenDuplicateBidder_ReturnsConflict()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 150.00m };

        // First bid
        await _controller.CreateBid(request);

        // Second bid from same bidder (placeholder-bidder-id)
        var duplicateRequest = new CreateBidRequest { AuctionId = 1, Amount = 160.00m };

        // Act
        var result = await _controller.CreateBid(duplicateRequest);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task GetMyBids_WhenCalled_ReturnsMyBidsResponse()
    {
        // Arrange
        // Create some test bids in database for the placeholder bidder
        var bidderId = "placeholder-bidder-id";
        var bidderIdHash = HashHelper.ComputeSha256Hash(bidderId);
        var bid1 = new Bid(1, 1, bidderId, bidderIdHash, new BidAmount(150.00m), DateTime.UtcNow.AddMinutes(-10));
        var bid2 = new Bid(2, 1, bidderId, bidderIdHash, new BidAmount(160.00m), DateTime.UtcNow.AddMinutes(-5));

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
        var highestBid = new Bid(3, 1, "other-bidder", "other-hash", new BidAmount(170.00m), DateTime.UtcNow);
        await _redisConnection.GetDatabase().HashSetAsync($"auction:1:highest", new HashEntry[]
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
}