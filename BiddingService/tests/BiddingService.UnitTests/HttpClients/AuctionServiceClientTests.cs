using BiddingService.Core.Interfaces;
using BiddingService.Infrastructure.HttpClients;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace BiddingService.UnitTests.HttpClients;

public class AuctionServiceClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<AuctionServiceClient>> _loggerMock;
    private readonly AuctionServiceClient _client;

    public AuctionServiceClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<AuctionServiceClient>>();
        _client = new AuctionServiceClient(_httpClient, _cache, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenAllAuctionsInCache_ReturnsFromCache()
    {
        // Arrange
        var auctionIds = new List<long> { 1, 2 };
        var cachedAuctions = new List<AuctionInfo>
        {
            new AuctionInfo { Id = 1, Title = "Auction 1", IsActive = true },
            new AuctionInfo { Id = 2, Title = "Auction 2", IsActive = true }
        };

        // Pre-populate cache
        foreach (var auction in cachedAuctions)
        {
            _cache.Set($"auction:{auction.Id}", auction, TimeSpan.FromMinutes(5));
        }

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(cachedAuctions);
        _httpMessageHandlerMock.VerifyNoOtherCalls(); // Should not make HTTP calls
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenSomeAuctionsInCache_FetchesMissingOnes()
    {
        // Arrange
        var auctionIds = new List<long> { 1, 2, 3 };
        var cachedAuction = new AuctionInfo { Id = 1, Title = "Auction 1", IsActive = true };
        var fetchedAuctions = new List<AuctionInfo>
        {
            new AuctionInfo { Id = 2, Title = "Auction 2", IsActive = true },
            new AuctionInfo { Id = 3, Title = "Auction 3", IsActive = true }
        };

        // Pre-populate cache with one auction
        _cache.Set($"auction:{cachedAuction.Id}", cachedAuction, TimeSpan.FromMinutes(5));

        // Setup HTTP response for batch request
        var responseContent = JsonSerializer.Serialize(fetchedAuctions);
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "http://localhost:5000/api/auctions/batch" &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(cachedAuction);
        result.Should().Contain(auction => auction.Id == 2 && auction.Title == "Auction 2");
        result.Should().Contain(auction => auction.Id == 3 && auction.Title == "Auction 3");

        // Verify fetched auctions are cached
        _cache.TryGetValue("auction:2", out AuctionInfo? cached2).Should().BeTrue();
        _cache.TryGetValue("auction:3", out AuctionInfo? cached3).Should().BeTrue();
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenBatchRequestFails_FallsBackToIndividualRequests()
    {
        // Arrange
        var auctionIds = new List<long> { 1, 2 };
        var auction1 = new AuctionInfo { Id = 1, Title = "Auction 1", IsActive = true };
        var auction2 = new AuctionInfo { Id = 2, Title = "Auction 2", IsActive = true };

        // Setup batch request to fail
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "http://localhost:5000/api/auctions/batch"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Setup individual requests to succeed
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().EndsWith("api/auctions/1") &&
                    req.Content == null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(auction1), System.Text.Encoding.UTF8, "application/json")
            });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().EndsWith("api/auctions/2") &&
                    req.Content == null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(auction2), System.Text.Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(auction => auction.Id == 1 && auction.Title == "Auction 1");
        result.Should().Contain(auction => auction.Id == 2 && auction.Title == "Auction 2");

        // Verify auctions are cached
        _cache.TryGetValue("auction:1", out AuctionInfo? cached1).Should().BeTrue();
        _cache.TryGetValue("auction:2", out AuctionInfo? cached2).Should().BeTrue();
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenBatchFailsAndIndividualRequestsFail_ReturnsEmptyList()
    {
        // Arrange
        var auctionIds = new List<long> { 1 };

        // Setup batch request to fail
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "http://localhost:5000/api/auctions/batch"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Setup individual request to fail
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "http://localhost:5000/api/auctions/1"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenEmptyAuctionIds_ReturnsEmptyList()
    {
        // Arrange
        var auctionIds = new List<long>();

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Assert
        result.Should().BeEmpty();
        _httpMessageHandlerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAuctionsBatchAsync_WhenBatchRequestThrowsException_FallsBackToIndividualRequests()
    {
        // Arrange
        var auctionIds = new List<long> { 1 };
        var auction1 = new AuctionInfo { Id = 1, Title = "Auction 1", IsActive = true };

        // Setup batch request to throw exception
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "http://localhost:5000/api/auctions/batch"),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Setup individual request to succeed
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "http://localhost:5000/api/auctions/1"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(auction1), System.Text.Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.GetAuctionsBatchAsync(auctionIds);

        // Assert
        result.Should().HaveCount(1);
        result.First().Should().BeEquivalentTo(auction1);
    }
}