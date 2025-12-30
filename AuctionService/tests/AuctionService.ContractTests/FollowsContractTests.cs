using AuctionService.ContractTests.Fixtures;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AuctionService.ContractTests;

/// <summary>
/// Follows API 契约测试 - 验证响应格式符合 OpenAPI 规范
/// </summary>
public class FollowsContractTests : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly WebApplicationFactoryFixture _fixture;
    private readonly HttpClient _client;

    public FollowsContractTests(WebApplicationFactoryFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetFollows_ReturnsResponseMatchingOpenApiSchema()
    {
        // Act - 取得追蹤清單（可能為空）
        var response = await _client.GetAsync("/api/follows");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Actual response: {content}"); // Debug output
        var jsonDocument = JsonDocument.Parse(content);

        // Assert - 验证響應結構 (success/message/data)
        jsonDocument.RootElement.TryGetProperty("success", out var success).Should().BeTrue();
        jsonDocument.RootElement.TryGetProperty("message", out var message).Should().BeTrue();
        jsonDocument.RootElement.TryGetProperty("data", out var data).Should().BeTrue();

        success.ValueKind.Should().Be(JsonValueKind.True);
        message.ValueKind.Should().Be(JsonValueKind.String);
        data.ValueKind.Should().Be(JsonValueKind.Object);

        // 验证 data 物件的結構
        data.TryGetProperty("items", out var items).Should().BeTrue();
        data.TryGetProperty("pageNumber", out var pageNumber).Should().BeTrue();
        data.TryGetProperty("pageSize", out var pageSize).Should().BeTrue();
        data.TryGetProperty("totalCount", out var totalCount).Should().BeTrue();
        data.TryGetProperty("totalPages", out var totalPages).Should().BeTrue();
        data.TryGetProperty("hasNextPage", out var hasNextPage).Should().BeTrue();
        data.TryGetProperty("hasPreviousPage", out var hasPreviousPage).Should().BeTrue();

        items.ValueKind.Should().Be(JsonValueKind.Array);
        pageNumber.ValueKind.Should().Be(JsonValueKind.Number);
        pageSize.ValueKind.Should().Be(JsonValueKind.Number);
        totalCount.ValueKind.Should().Be(JsonValueKind.Number);
        totalPages.ValueKind.Should().Be(JsonValueKind.Number);
        hasNextPage.ValueKind.Should().Be(JsonValueKind.False);
        hasPreviousPage.ValueKind.Should().Be(JsonValueKind.False);

        // 验证分頁默認值
        pageNumber.GetInt32().Should().Be(1);
        pageSize.GetInt32().Should().Be(10);
        totalCount.GetInt32().Should().Be(0);
        totalPages.GetInt32().Should().Be(0);
        items.GetArrayLength().Should().Be(0);
    }
}