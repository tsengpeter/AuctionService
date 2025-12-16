using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;
using AuctionService.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AuctionService.IntegrationTests.Controllers;

/// <summary>
/// API 響應包裝類型
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

/// <summary>
/// CreatedAtAction 回應包裝類
/// </summary>
public class CreatedAtActionResponse<T>
{
    public T? Value { get; set; }
    public string[]? Formatters { get; set; }
    public string[]? ContentTypes { get; set; }
    public string? DeclaredType { get; set; }
    public int StatusCode { get; set; }
}

/// <summary>
/// FollowsController 整合測試
/// </summary>
public class FollowsControllerIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;
    private readonly HttpClient _client;

    public FollowsControllerIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private async Task<Guid> GetTestAuctionIdAsync()
    {
        // 返回預先插入的測試拍賣 ID
        return Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public async Task FollowAuction_WithValidAuction_ReturnsCreated()
    {
        // Arrange - 獲取預先插入的測試拍賣 ID（由其他用戶創建）
        var auctionId = await GetTestAuctionIdAsync();

        // Act - 追蹤商品
        var followRequest = new FollowAuctionRequest
        {
            AuctionId = auctionId
        };

        var followResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);

        // Assert
        followResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdResponse = await followResponse.Content.ReadFromJsonAsync<ApiResponse<FollowDto>>();
        createdResponse.Should().NotBeNull();
        createdResponse!.Success.Should().BeTrue();
        createdResponse.Data.Should().NotBeNull();

        var followDto = createdResponse.Data!;
        followDto.AuctionId.Should().Be(auctionId);
    }

    [Fact]
    public async Task FollowAuction_WithOwnAuction_ReturnsBadRequest()
    {
        // Arrange - 先創建一個拍賣商品
        var createAuctionRequest = new CreateAuctionRequest
        {
            Name = "Test Auction for Following",
            Description = "This auction will be followed",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(1),
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/auctions", createAuctionRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdResponse = await createResponse.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        createdResponse.Should().NotBeNull();
        createdResponse!.Data.Should().NotBeNull();
        var auctionId = createdResponse.Data!.Id;

        // Act - 嘗試追蹤自己的商品
        var followRequest = new FollowAuctionRequest
        {
            AuctionId = auctionId
        };

        var followResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);

        // Assert
        followResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // For error responses, directly deserialize the content
        var errorResponse = await followResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Success.Should().BeFalse();
        errorResponse.StatusCode.ToString().Should().Be("VALIDATION_ERROR");
        errorResponse.Message.Should().Contain("Cannot follow your own");
    }

    [Fact]
    public async Task FollowAuction_WithDuplicateFollow_ReturnsConflict()
    {
        // Arrange - 使用測試拍賣並確保它是未追蹤的狀態
        var auctionId = await GetTestAuctionIdAsync();

        // 先取消追蹤（如果存在的話）
        await _client.DeleteAsync($"/api/follows/{auctionId}");

        // 第一次追蹤
        var followRequest = new FollowAuctionRequest { AuctionId = auctionId };
        var firstFollowResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);
        firstFollowResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - 再次追蹤同一個商品
        var secondFollowResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);

        // Assert
        secondFollowResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await secondFollowResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Success.Should().BeFalse();
        errorResponse.StatusCode.Should().Be("DUPLICATE_FOLLOW");
        errorResponse.Message.Should().Contain("Already following");
    }

    [Fact]
    public async Task FollowAuction_WithSelfFollow_ReturnsBadRequest()
    {
        // Arrange - 創建一個拍賣商品（使用同一個用戶）
        var createAuctionRequest = new CreateAuctionRequest
        {
            Name = "Test Auction for Self Follow",
            Description = "This auction will be self-followed",
            StartingPrice = 200.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(1),
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/auctions", createAuctionRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdResponse = await createResponse.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        var auctionId = createdResponse!.Data!.Id;

        // Act - 嘗試追蹤自己的商品
        var followRequest = new FollowAuctionRequest { AuctionId = auctionId };
        var followResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);

        // Assert
        followResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await followResponse.Content.ReadAsStringAsync();
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.Success.Should().BeFalse();
        errorResponse.StatusCode.Should().Be("VALIDATION_ERROR");
        errorResponse.Message.Should().Contain("Cannot follow your own");
    }

    [Fact]
    public async Task UnfollowAuction_WithExistingFollow_ReturnsNoContent()
    {
        // Arrange - 追蹤一個測試拍賣
        var auctionId = await GetTestAuctionIdAsync();

        // 先取消追蹤（如果存在的話）
        await _client.DeleteAsync($"/api/follows/{auctionId}");

        var followRequest = new FollowAuctionRequest { AuctionId = auctionId };
        var followResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);
        followResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - 取消追蹤
        var unfollowResponse = await _client.DeleteAsync($"/api/follows/{auctionId}");

        // Assert
        unfollowResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetFollows_WithExistingFollows_ReturnsFollowList()
    {
        // Arrange - 追蹤多個測試拍賣
        var testAuctionIds = new[]
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        foreach (var auctionId in testAuctionIds)
        {
            // 先取消追蹤（如果存在的話）
            await _client.DeleteAsync($"/api/follows/{auctionId}");

            // 追蹤商品
            var followRequest = new FollowAuctionRequest { AuctionId = auctionId };
            var followResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);
            followResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Act - 取得追蹤清單
        var getResponse = await _client.GetAsync("/api/follows");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 解析完整的 API 響應結構
        var apiResponse = await getResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FollowDto>>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();

        var followsResult = apiResponse.Data!;
        followsResult.Items.Should().HaveCount(3);

        // 驗證追蹤的商品資訊
        foreach (var follow in followsResult.Items)
        {
            testAuctionIds.Should().Contain(follow.AuctionId);
            follow.AuctionName.Should().StartWith("Test Auction ");
        }
    }
}