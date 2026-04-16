using Auction.Infrastructure.Persistence;
using AuctionService.IntegrationTests.Infrastructure;
using AuctionService.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.IntegrationTests.Auction;

[Collection("Integration")]
public class GetAuctionDetailIntegrationTests(IntegrationTestFixture fixture)
{
    private async Task<AuctionEntity> SeedAuctionAsync(string title = "Detail Test Auction")
    {
        var auction = AuctionEntity.Create(
            Guid.NewGuid(), title, "A description", 200m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(5),
            null, null);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
        return auction;
    }

    [Fact]
    public async Task GetAuctionById_ExistingId_Returns200WithDetail()
    {
        var auction = await SeedAuctionAsync();

        var response = await fixture.Client.GetAsync($"/api/auctions/{auction.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailResult>>();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(auction.Id);
        body.Data.Title.Should().Be("Detail Test Auction");
    }

    [Fact]
    public async Task GetAuctionById_NonExistentId_Returns404()
    {
        var response = await fixture.Client.GetAsync($"/api/auctions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper class for deserialization
    private class AuctionDetailResult
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal StartingPrice { get; init; }
        public decimal? CurrentHighestBid { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
