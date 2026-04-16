using Auction.Infrastructure.Persistence;
using AuctionService.IntegrationTests.Infrastructure;
using AuctionService.IntegrationTests.Member;
using AuctionService.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.IntegrationTests.Auction;

[Collection("Integration")]
public class UpdateAuctionIntegrationTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    private async Task<(AuctionEntity Auction, string Token)> CreateAuctionWithTokenAsync(
        DateTimeOffset? startTime = null)
    {
        var token = await RegisterAndLoginAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

        var requestBody = new
        {
            title = $"Update_{Guid.NewGuid():N}",
            description = (string?)null,
            startingPrice = 100m,
            startTime = (startTime ?? DateTimeOffset.UtcNow.AddHours(2)).ToString("O"),
            endTime = DateTimeOffset.UtcNow.AddDays(7).ToString("O"),
            categoryId = (Guid?)null,
            imageUrls = (List<string>?)null
        };

        var createResponse = await Client.PostAsJsonAsync("/api/auctions", requestBody);
        Client.DefaultRequestHeaders.Authorization = null;
        createResponse.EnsureSuccessStatusCode();

        var createBody = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var id = createBody!.Data;

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var auction = await db.Auctions.FindAsync(id);
        return (auction!, token!);
    }

    [Fact]
    public async Task PutAuction_WithoutAuth_Returns401()
    {
        var (auction, _) = await CreateAuctionWithTokenAsync();
        Client.DefaultRequestHeaders.Authorization = null;

        var response = await Client.PutAsJsonAsync($"/api/auctions/{auction.Id}",
            new { title = "New Title" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutAuction_BeforeStartTime_AllFieldsUpdated_Returns200()
    {
        var (auction, token) = await CreateAuctionWithTokenAsync(DateTimeOffset.UtcNow.AddHours(2));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PutAsJsonAsync($"/api/auctions/{auction.Id}", new
        {
            title = "Updated Title",
            startingPrice = 250m
        });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailResult>>();
        body!.Data!.Title.Should().Be("Updated Title");
        body.Data.StartingPrice.Should().Be(250m);
    }

    [Fact]
    public async Task PutAuction_NonExistentAuction_Returns404()
    {
        var token = await RegisterAndLoginAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

        var response = await Client.PutAsJsonAsync($"/api/auctions/{Guid.NewGuid()}",
            new { title = "Title" });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutAuction_NonOwner_Returns403()
    {
        var (auction, _) = await CreateAuctionWithTokenAsync();
        var otherToken = await RegisterAndLoginAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken!);

        var response = await Client.PutAsJsonAsync($"/api/auctions/{auction.Id}",
            new { title = "Hijacked" });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PutAuction_EndedAuction_Returns409()
    {
        var (auction, token) = await CreateAuctionWithTokenAsync();
        // Force end the auction directly in DB
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var dbAuction = await db.Auctions.FindAsync(auction.Id);
        dbAuction!.End(null, null);
        await db.SaveChangesAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await Client.PutAsJsonAsync($"/api/auctions/{auction.Id}",
            new { title = "New Title" });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PutAuction_AfterStartTime_StartingPriceChange_Returns422()
    {
        // Seed an auction with startTime in the past to simulate bidding started
        var uniqueTitle = $"PastStart_{Guid.NewGuid():N}";
        var token = await RegisterAndLoginAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

        // Create via API first (with future startTime), then manually update DB
        var createResponse = await Client.PostAsJsonAsync("/api/auctions", new
        {
            title = uniqueTitle,
            startingPrice = 50m,
            startTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            endTime = DateTimeOffset.UtcNow.AddDays(7).ToString("O")
        });
        Client.DefaultRequestHeaders.Authorization = null;
        var createBody = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var auctionId = createBody!.Data;

        // Manually set startTime to past to simulate bidding started
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var auction = await db.Auctions.FindAsync(auctionId);
        auction!.UpdateAll(auction.Title, auction.Description, auction.StartingPrice,
            DateTimeOffset.UtcNow.AddHours(-1), auction.EndTime, auction.CategoryId, null);
        await db.SaveChangesAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await Client.PutAsJsonAsync($"/api/auctions/{auctionId}",
            new { startingPrice = 999m });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private class AuctionDetailResult
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public decimal StartingPrice { get; init; }
    }
}
