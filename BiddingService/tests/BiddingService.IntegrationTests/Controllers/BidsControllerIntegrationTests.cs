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
using Xunit;

namespace BiddingService.IntegrationTests.Controllers;

public class BidsControllerIntegrationTests : IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private readonly IContainer _redisContainer;
    private readonly WireMockServer _auctionServiceMock;
    private readonly BiddingDbContext _dbContext;
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly BidsController _controller;

    public BidsControllerIntegrationTests()
    {
        // Start PostgreSQL container
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:14")
            .WithEnvironment("POSTGRES_DB", "bidding_test")
            .WithEnvironment("POSTGRES_USER", "testuser")
            .WithEnvironment("POSTGRES_PASSWORD", "testpass")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        // Start Redis container
        _redisContainer = new ContainerBuilder()
            .WithImage("redis:7")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();

        // Start WireMock for auction service
        _auctionServiceMock = WireMockServer.Start();

        // Setup database context
        var postgresConnectionString = $"Host=localhost;Port={_postgresContainer.GetMappedPublicPort(5432)};Database=bidding_test;Username=testuser;Password=testpass";
        var dbContextOptions = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(postgresConnectionString)
            .Options;
        _dbContext = new BiddingDbContext(dbContextOptions);

        // Setup Redis connection
        var redisConnectionString = $"localhost:{_redisContainer.GetMappedPublicPort(6379)}";
        _redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

        // Setup services
        var logger = new LoggerFactory().CreateLogger<BiddingService.Core.Services.BiddingService>();
        var idGenerator = new SnowflakeIdGenerator(1, 1);
        var encryptionService = new BiddingService.Infrastructure.Encryption.EncryptionService(
            "0123456789abcdef0123456789abcdef",
            "0123456789abcdef");

        var bidRepository = new BidRepository(_dbContext);
        var redisRepository = new RedisRepository(_redisConnection, encryptionService);

        var auctionServiceClient = new AuctionServiceClient(
            new HttpClient { BaseAddress = new Uri(_auctionServiceMock.Url) },
            new MemoryCache(new MemoryCacheOptions()),
            new LoggerFactory().CreateLogger<AuctionServiceClient>());

        var biddingService = new BiddingService.Core.Services.BiddingService(
            bidRepository,
            redisRepository,
            auctionServiceClient,
            idGenerator,
            encryptionService,
            logger);

        _controller = new BidsController(biddingService, new LoggerFactory().CreateLogger<BidsController>());
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Ensure database is created and migrated
        await _dbContext.Database.EnsureCreatedAsync();

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
        _redisConnection.Dispose();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateBid_WhenValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 150.00m };
        var bidderId = "test-bidder-123";

        // Act
        var result = await _controller.CreateBid(request, bidderId);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeOfType<BidResponse>();

        var bidResponse = createdResult.Value as BidResponse;
        bidResponse.AuctionId.Should().Be(1);
        bidResponse.Amount.Should().Be(150.00m);
        bidResponse.BidderId.Should().Be(bidderId);
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
        var bidderId = "test-bidder-123";

        // Act
        var result = await _controller.CreateBid(request, bidderId);

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
        await _controller.CreateBid(highBidRequest, "other-bidder");

        // Now try a lower bid
        var lowBidRequest = new CreateBidRequest { AuctionId = 1, Amount = 150.00m };
        var bidderId = "test-bidder-123";

        // Act
        var result = await _controller.CreateBid(lowBidRequest, bidderId);

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
        var bidderId = "test-bidder-123";

        // First bid
        await _controller.CreateBid(request, bidderId);

        // Second bid from same bidder
        var duplicateRequest = new CreateBidRequest { AuctionId = 1, Amount = 160.00m };

        // Act
        var result = await _controller.CreateBid(duplicateRequest, bidderId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(409);
    }
}