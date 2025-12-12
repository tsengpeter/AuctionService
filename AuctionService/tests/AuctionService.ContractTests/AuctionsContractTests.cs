using AuctionService.ContractTests.Fixtures;
using AuctionService.Core.DTOs.Requests;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NJsonSchema;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AuctionService.ContractTests;

/// <summary>
/// API 契约测试 - 验证响应格式符合 OpenAPI 规范
/// </summary>
public class AuctionsContractTests : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly WebApplicationFactoryFixture _fixture;
    private readonly HttpClient _client;

    public AuctionsContractTests(WebApplicationFactoryFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAuctions_ReturnsResponseMatchingOpenApiSchema()
    {
        // Act
        var response = await _client.GetAsync("/api/auctions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Actual response: {content}"); // Debug output
        var jsonDocument = JsonDocument.Parse(content);

        // Assert - 验证实际的响应结构 (success/message/data)
        jsonDocument.RootElement.TryGetProperty("success", out var success).Should().BeTrue();
        jsonDocument.RootElement.TryGetProperty("message", out var message).Should().BeTrue();
        jsonDocument.RootElement.TryGetProperty("data", out var data).Should().BeTrue();

        // Assert - 验证 success 为 true
        success.ValueKind.Should().Be(JsonValueKind.True);

        // Assert - 验证 data 结构
        data.TryGetProperty("items", out var items).Should().BeTrue();
        data.TryGetProperty("pageNumber", out var pageNumber).Should().BeTrue();
        data.TryGetProperty("pageSize", out var pageSize).Should().BeTrue();
        data.TryGetProperty("totalCount", out var totalCount).Should().BeTrue();
        data.TryGetProperty("totalPages", out var totalPages).Should().BeTrue();

        // Assert - 验证 items 是数组
        items.ValueKind.Should().Be(JsonValueKind.Array);

        // 如果有項目，验证第一个項目的结构
        if (items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            firstItem.TryGetProperty("id", out _).Should().BeTrue();
            firstItem.TryGetProperty("title", out _).Should().BeTrue();
            firstItem.TryGetProperty("startingPrice", out _).Should().BeTrue();
            firstItem.TryGetProperty("startTime", out _).Should().BeTrue();
            firstItem.TryGetProperty("endTime", out _).Should().BeTrue();
            firstItem.TryGetProperty("status", out _).Should().BeTrue();
            firstItem.TryGetProperty("seller", out _).Should().BeTrue();
            firstItem.TryGetProperty("category", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateAuction_ReturnsResponseMatchingOpenApiSchema()
    {
        // Arrange
        var createRequest = new CreateAuctionRequest
        {
            Name = "Contract Test Auction",
            Description = "Testing API contract compliance",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(1),
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auctions", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Create response: {content}"); // Debug output
        var jsonDocument = JsonDocument.Parse(content);

        // The response is wrapped in ASP.NET Core's ObjectResult format
        jsonDocument.RootElement.TryGetProperty("value", out var value).Should().BeTrue();

        // Assert - 验证实际的响应结构 (success/message/data)
        value.TryGetProperty("success", out var success).Should().BeTrue();
        value.TryGetProperty("message", out var message).Should().BeTrue();
        value.TryGetProperty("data", out var data).Should().BeTrue();

        // Assert - 验证 success 为 true
        success.ValueKind.Should().Be(JsonValueKind.True);

        // Assert - 验证 message
        message.ValueKind.Should().Be(JsonValueKind.String);
        message.GetString().Should().Be("商品建立成功");

        // Assert - 验证 data 结构 (AuctionDetailDto)
        data.TryGetProperty("id", out _).Should().BeTrue();
        data.TryGetProperty("title", out _).Should().BeTrue();
        data.TryGetProperty("description", out _).Should().BeTrue();
        data.TryGetProperty("startingPrice", out _).Should().BeTrue();
        data.TryGetProperty("currentPrice", out _).Should().BeTrue();
        data.TryGetProperty("startTime", out _).Should().BeTrue();
        data.TryGetProperty("endTime", out _).Should().BeTrue();
        data.TryGetProperty("status", out _).Should().BeTrue();
        data.TryGetProperty("seller", out _).Should().BeTrue();
        data.TryGetProperty("bids", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAuctionById_ReturnsResponseMatchingOpenApiSchema()
    {
        // Arrange - 先创建一个拍卖
        var createRequest = new CreateAuctionRequest
        {
            Name = "Contract Test Auction for GetById",
            Description = "Testing GET by ID contract",
            StartingPrice = 50.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(1),
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/auctions", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var auctionId = createJson.RootElement.GetProperty("value").GetProperty("data").GetProperty("id").GetString();

        // Act
        var response = await _client.GetAsync($"/api/auctions/{auctionId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"GetById response: {content}"); // Debug output
        var jsonDocument = JsonDocument.Parse(content);

        // Assert - 验证实际的响应结构 (success/message/data)
        jsonDocument.RootElement.TryGetProperty("success", out var success).Should().BeTrue();
        jsonDocument.RootElement.TryGetProperty("message", out var message).Should().BeTrue();
        jsonDocument.RootElement.TryGetProperty("data", out var data).Should().BeTrue();

        // Assert - 验证 success 为 true
        success.ValueKind.Should().Be(JsonValueKind.True);

        // Assert - 验证 data 结构 (AuctionDetailDto)
        data.TryGetProperty("id", out _).Should().BeTrue();
        data.TryGetProperty("title", out _).Should().BeTrue();
        data.TryGetProperty("description", out _).Should().BeTrue();
        data.TryGetProperty("startingPrice", out _).Should().BeTrue();
        data.TryGetProperty("currentPrice", out _).Should().BeTrue();
        data.TryGetProperty("startTime", out _).Should().BeTrue();
        data.TryGetProperty("endTime", out _).Should().BeTrue();
        data.TryGetProperty("status", out _).Should().BeTrue();
        data.TryGetProperty("seller", out _).Should().BeTrue();
        data.TryGetProperty("bids", out _).Should().BeTrue();
    }
}