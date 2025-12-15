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
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
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

        var followDto = await followResponse.Content.ReadFromJsonAsync<FollowDto>();
        followDto.Should().NotBeNull();
        followDto!.AuctionId.Should().Be(auctionId);
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

        var auctionResponse = await createResponse.Content.ReadFromJsonAsync<AuctionDetailDto>();
        var auctionId = auctionResponse!.Id;

        // Act - 嘗試追蹤自己的商品
        var followRequest = new FollowAuctionRequest
        {
            AuctionId = auctionId
        };

        var followResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);

        // Assert
        followResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

        var auctionResponse = await createResponse.Content.ReadFromJsonAsync<AuctionDetailDto>();
        var auctionId = auctionResponse!.Id;

        // Act - 嘗試追蹤自己的商品
        var followRequest = new FollowAuctionRequest { AuctionId = auctionId };
        var followResponse = await _client.PostAsJsonAsync("/api/follows", followRequest);

        // Assert
        followResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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