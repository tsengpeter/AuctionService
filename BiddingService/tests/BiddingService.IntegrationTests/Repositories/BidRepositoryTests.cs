using BiddingService.Core.Entities;
using BiddingService.Core.ValueObjects;
using BiddingService.Infrastructure.Data;
using BiddingService.Infrastructure.Repositories;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BiddingService.IntegrationTests.Repositories;

public class BidRepositoryTests : IAsyncLifetime
{
    private IContainer _postgresContainer;
    private BiddingDbContext _dbContext;
    private BidRepository _repository;

    public BidRepositoryTests()
    {
        // Create PostgreSQL container (don't start yet)
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:16")
            .WithEnvironment("POSTGRES_DB", "bidding_test")
            .WithEnvironment("POSTGRES_USER", "testuser")
            .WithEnvironment("POSTGRES_PASSWORD", "testpass")
            .WithPortBinding(5432, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgresContainer.StartAsync();

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

        // Create repository
        _repository = new BidRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        // Clean up
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
        _dbContext?.Dispose();
    }

    [Fact]
    public async Task GetBidsByAuctionAsync_WhenBidsExist_ReturnsBidsForAuction()
    {
        // Arrange
        var auctionId = 1;
        var bidderId1 = "bidder1";
        var bidderId2 = "bidder2";

        // Create test bids
        var bid1 = new Bid(1, auctionId, bidderId1, "hash1", new BidAmount(100.00m), DateTime.UtcNow.AddMinutes(-5));
        var bid2 = new Bid(2, auctionId, bidderId2, "hash2", new BidAmount(150.00m), DateTime.UtcNow.AddMinutes(-3));
        var bid3 = new Bid(3, 2, bidderId1, "hash1", new BidAmount(200.00m), DateTime.UtcNow.AddMinutes(-1)); // Different auction

        await _repository.AddAsync(bid1);
        await _repository.AddAsync(bid2);
        await _repository.AddAsync(bid3);

        // Act
        var result = await _repository.GetBidsByAuctionAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var bidsList = result.ToList();
        bidsList[0].AuctionId.Should().Be(auctionId);
        bidsList[0].BidderId.Should().Be(bidderId2); // Most recent first
        bidsList[0].Amount.Value.Should().Be(150.00m);

        bidsList[1].AuctionId.Should().Be(auctionId);
        bidsList[1].BidderId.Should().Be(bidderId1);
        bidsList[1].Amount.Value.Should().Be(100.00m);
    }

    [Fact]
    public async Task GetBidsByAuctionAsync_WhenNoBidsExist_ReturnsEmptyCollection()
    {
        // Arrange
        var auctionId = 999; // Auction with no bids

        // Act
        var result = await _repository.GetBidsByAuctionAsync(auctionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBidsByAuctionAsync_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var auctionId = 1;
        var bidderId = "bidder1";

        // Create multiple bids (note: BidAt is set so that higher ID = older bid)
        for (int i = 1; i <= 10; i++)
        {
            var bid = new Bid(i, auctionId, bidderId, $"hash{i}", new BidAmount(100.00m * i), DateTime.UtcNow.AddMinutes(-i));
            await _repository.AddAsync(bid);
        }

        // Act - Get first page (page 1, pageSize 5)
        var result = await _repository.GetBidsByAuctionAsync(auctionId, 1, 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);

        // Should be ordered by BidAt descending (most recent first)
        // ID 1 has BidAt = UtcNow.AddMinutes(-1) (most recent)
        // ID 2 has BidAt = UtcNow.AddMinutes(-2)
        // ...
        // ID 5 has BidAt = UtcNow.AddMinutes(-5)
        var bidsList = result.ToList();
        bidsList[0].Amount.Value.Should().Be(100.00m); // Most recent (ID 1)
        bidsList[1].Amount.Value.Should().Be(200.00m); // ID 2
        bidsList[2].Amount.Value.Should().Be(300.00m); // ID 3
        bidsList[3].Amount.Value.Should().Be(400.00m); // ID 4
        bidsList[4].Amount.Value.Should().Be(500.00m); // ID 5
    }

    [Fact]
    public async Task GetBidCountAsync_WhenBidsExist_ReturnsCorrectCount()
    {
        // Arrange
        var auctionId = 1;
        var bidderId = "bidder1";

        // Create test bids
        var bid1 = new Bid(1, auctionId, bidderId, "hash1", new BidAmount(100.00m), DateTime.UtcNow);
        var bid2 = new Bid(2, auctionId, bidderId, "hash2", new BidAmount(150.00m), DateTime.UtcNow);
        var bid3 = new Bid(3, 2, bidderId, "hash3", new BidAmount(200.00m), DateTime.UtcNow); // Different auction

        await _repository.AddAsync(bid1);
        await _repository.AddAsync(bid2);
        await _repository.AddAsync(bid3);

        // Act
        var result = await _repository.GetBidCountAsync(auctionId);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetBidCountAsync_WhenNoBidsExist_ReturnsZero()
    {
        // Arrange
        var auctionId = 999; // Auction with no bids

        // Act
        var result = await _repository.GetBidCountAsync(auctionId);

        // Assert
        result.Should().Be(0);
    }
}