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
public class WatchlistIntegrationTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    private async Task<AuctionEntity> SeedAuctionAsync(string title = "Watchlist Test Auction")
    {
        var auction = AuctionEntity.Create(
            Guid.NewGuid(), title, null, 75m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(5),
            null, null);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
        return auction;
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var token = await RegisterAndLoginAsync();
        token.Should().NotBeNullOrEmpty();
        return token!;
    }

    [Fact]
    public async Task PostWatchlist_WithoutAuth_Returns401()
    {
        var auction = await SeedAuctionAsync();
        Client.DefaultRequestHeaders.Authorization = null;

        var response = await Client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostWatchlist_AuthenticatedUser_Returns204()
    {
        var auction = await SeedAuctionAsync();
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PostWatchlist_DuplicateAdd_IsIdempotent()
    {
        var auction = await SeedAuctionAsync();
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await Client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);
        var response = await Client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteWatchlist_RemovesItem_Returns204()
    {
        var auction = await SeedAuctionAsync();
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await Client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);
        var response = await Client.DeleteAsync($"/api/auctions/{auction.Id}/watchlist");
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetWatchlist_AfterAddingItem_ReturnsItem()
    {
        var auction = await SeedAuctionAsync($"GetWatchlistTest_{Guid.NewGuid():N}");
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await Client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);
        var response = await Client.GetAsync("/api/watchlist");
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<WatchlistItemResult>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostWatchlist_NonExistentAuction_Returns404()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PostAsync($"/api/auctions/{Guid.NewGuid()}/watchlist", null);
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private class WatchlistItemResult
    {
        public Guid AuctionId { get; init; }
        public string Title { get; init; } = string.Empty;
    }
}
