using BiddingService.Core.Interfaces;
using BiddingService.Infrastructure.HttpClients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.RegularExpressions;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace BiddingService.IntegrationTests.Contracts;

public class AuctionServiceContractTests : IAsyncLifetime
{
    private readonly WireMockServer _mockServer;
    private readonly AuctionServiceClient _client;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<AuctionServiceClient>> _loggerMock;

    public AuctionServiceContractTests()
    {
        _mockServer = WireMockServer.Start();
        // Use real MemoryCache instead of mock to avoid Moq issues with out parameters
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<AuctionServiceClient>>();

        // Create client with mock server URL
        var httpClient = new HttpClient { BaseAddress = new Uri(_mockServer.Url) };
        _client = new AuctionServiceClient(httpClient, _cache, _loggerMock.Object);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _mockServer.Stop();
        _mockServer.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAuctionAsync_WhenAuctionExists_ReturnsAuctionInfo()
    {
        // Arrange
        var auctionId = 123L;
        var expectedAuction = new AuctionInfo
        {
            Id = auctionId,
            Title = "Test Auction",
            IsActive = true,
            StartingPrice = 100.00m,
            EndTime = DateTime.UtcNow.AddDays(1)
        };

        _mockServer
            .Given(Request.Create().WithPath($"/api/auctions/{auctionId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"
                {{
                    ""id"": {auctionId},
                    ""title"": ""Test Auction"",
                    ""isActive"": true,
                    ""startingPrice"": 100.00,
                    ""endTime"": ""{expectedAuction.EndTime:O}""
                }}"));

        // Act
        var result = await _client.GetAuctionAsync(auctionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAuction.Id, result.Id);
        Assert.Equal(expectedAuction.Title, result.Title);
        Assert.Equal(expectedAuction.IsActive, result.IsActive);
        Assert.Equal(expectedAuction.StartingPrice, result.StartingPrice);
    }

    [Fact]
    public async Task GetAuctionAsync_WhenAuctionDoesNotExist_ReturnsNull()
    {
        // Arrange
        var auctionId = 999L;

        _mockServer
            .Given(Request.Create().WithPath($"/api/auctions/{auctionId}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        // Act
        var result = await _client.GetAuctionAsync(auctionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenAuctionsExist_ReturnsAuctionInfos()
    {
        // Arrange
        var auctionIds = new[] { 123L, 456L };
        var expectedAuctions = new[]
        {
            new AuctionInfo
            {
                Id = 123L,
                Title = "Auction 1",
                IsActive = true,
                StartingPrice = 100.00m,
                EndTime = DateTime.UtcNow.AddDays(1)
            },
            new AuctionInfo
            {
                Id = 456L,
                Title = "Auction 2",
                IsActive = false,
                StartingPrice = 200.00m,
                EndTime = DateTime.UtcNow.AddDays(2)
            }
        };

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/auctions/batch")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"
                [
                    {{
                        ""id"": 123,
                        ""title"": ""Auction 1"",
                        ""isActive"": true,
                        ""startingPrice"": 100.00,
                        ""endTime"": ""{expectedAuctions[0].EndTime:O}""
                    }},
                    {{
                        ""id"": 456,
                        ""title"": ""Auction 2"",
                        ""isActive"": false,
                        ""startingPrice"": 200.00,
                        ""endTime"": ""{expectedAuctions[1].EndTime:O}""
                    }}
                ]"));

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Debug: Check if WireMock received any requests
        var logEntries = _mockServer.LogEntries;
        Assert.NotEmpty(logEntries); // Ensure at least one request was made

        // Assert
        Assert.NotNull(result);
        var auctions = result.ToList();
        Assert.Equal(2, auctions.Count);
        Assert.Equal(expectedAuctions[0].Id, auctions[0].Id);
        Assert.Equal(expectedAuctions[0].Title, auctions[0].Title);
        Assert.Equal(expectedAuctions[1].Id, auctions[1].Id);
        Assert.Equal(expectedAuctions[1].Title, auctions[1].Title);
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenSomeAuctionsDoNotExist_ReturnsOnlyExistingAuctions()
    {
        // Arrange
        var auctionIds = new[] { 123L, 999L }; // 999 doesn't exist

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/auctions/batch")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"
                [
                    {{
                        ""id"": 123,
                        ""title"": ""Auction 1"",
                        ""isActive"": true,
                        ""startingPrice"": 100.00,
                        ""endTime"": ""{DateTime.UtcNow.AddDays(1):O}""
                    }}
                ]"));

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Assert
        Assert.NotNull(result);
        var auctions = result.ToList();
        Assert.Single(auctions);
        Assert.Equal(123L, auctions[0].Id);
    }
}
