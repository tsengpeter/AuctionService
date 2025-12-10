using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;
using AuctionService.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AuctionService.IntegrationTests.Controllers;

/// <summary>
/// AuctionsController 整合測試
/// </summary>
public class AuctionsControllerIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;
    private readonly HttpClient _client;

    public AuctionsControllerIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task CreateAuction_WithEndTimeInFuture_WaitUntilEnded_ReturnsStatusEnded()
    {
        // Arrange - 創建即將結束的拍賣（2秒後結束）
        var createRequest = new
        {
            Name = "Test Auction Ending Soon",
            Description = "This auction will end in 2 seconds",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddSeconds(1),
            EndTime = DateTime.UtcNow.AddSeconds(2) // 2秒後結束
        };

        // Act - 創建拍賣
        var createResponse = await _client.PostAsJsonAsync("/api/auctions", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdAuction = await createResponse.Content.ReadFromJsonAsync<AuctionDetailDto>();
        createdAuction.Should().NotBeNull();

        // 等待3秒讓拍賣結束
        await Task.Delay(3000);

        // Act - 查詢拍賣清單
        var getResponse = await _client.GetAsync("/api/auctions");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auctions = await getResponse.Content.ReadFromJsonAsync<PagedResult<AuctionListItemDto>>();
        auctions.Should().NotBeNull();
        auctions!.Items.Should().NotBeEmpty();

        // Assert - 驗證拍賣狀態為 Ended
        var endedAuction = auctions.Items.FirstOrDefault(a => a.Id == createdAuction!.Id);
        endedAuction.Should().NotBeNull();
        endedAuction!.Status.Should().Be(AuctionStatus.Ended);
    }
}