using BiddingService.Core.Entities;
using BiddingService.Core.ValueObjects;
using BiddingService.Core.Interfaces;
using BiddingService.Infrastructure.Data;
using BiddingService.Infrastructure.Repositories;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using Xunit;

namespace BiddingService.IntegrationTests.Repositories;

public class BidRepositoryTests : IAsyncLifetime
{
    private IContainer? _postgresContainer;
    private BiddingDbContext _dbContext;
    private BidRepository _repository;
    private Mock<IEncryptionService> _encryptionServiceMock;
    private readonly bool _useTestcontainers;
    private string _postgresConnectionString = string.Empty;

    public BidRepositoryTests()
    {
        _encryptionServiceMock = new Mock<IEncryptionService>();

        // Check if running in CI/CD environment
        var ciPostgresConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        _useTestcontainers = string.IsNullOrEmpty(ciPostgresConnection);

        if (_useTestcontainers)
        {
            // Local development: use Testcontainers
            _postgresContainer = new ContainerBuilder()
                .WithImage("postgres:16")
                .WithEnvironment("POSTGRES_DB", "bidding_test")
                .WithEnvironment("POSTGRES_USER", "testuser")
                .WithEnvironment("POSTGRES_PASSWORD", "testpass")
                .WithPortBinding(5432, true)
                .Build();
        }
        else
        {
            // CI/CD: use pre-configured PostgreSQL service
            _postgresConnectionString = ciPostgresConnection!;
        }
    }

    public async Task InitializeAsync()
    {
        // Setup mock encryption service
        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns((string input) => $"encrypted_{input}");
        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string input) => input.Replace("encrypted_", ""));

        if (_useTestcontainers && _postgresContainer != null)
        {
            // Start PostgreSQL container for local development
            await _postgresContainer.StartAsync();

            // Wait for PostgreSQL to be ready with retry logic
            await WaitForPostgresReady();

            _postgresConnectionString = $"Host={_postgresContainer.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};Database=bidding_test;Username=testuser;Password=testpass;SSL Mode=Disable";
        }

        // Setup database context
        var dbContextOptions = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(_postgresConnectionString)
            .Options;
        _dbContext = new BiddingDbContext(dbContextOptions, _encryptionServiceMock.Object);

        // Ensure database is created
        await _dbContext.Database.EnsureCreatedAsync();

        // Create repository
        _repository = new BidRepository(_dbContext);
    }

    private async Task WaitForPostgresReady()
    {
        if (!_useTestcontainers)
        {
            // CI/CD environment: assume PostgreSQL is already ready
            return;
        }

        var connectionString = $"Host={_postgresContainer!.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};Database=bidding_test;Username=testuser;Password=testpass;SSL Mode=Disable";
        
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

    public async Task DisposeAsync()
    {
        // Clean up
        if (_useTestcontainers && _postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }
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