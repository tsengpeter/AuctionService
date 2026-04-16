using Auction.Domain.Entities;
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
public class GetAuctionsIntegrationTests(IntegrationTestFixture fixture)
{
    private static AuctionEntity CreateActiveAuction(string title, Guid? categoryId = null) =>
        AuctionEntity.Create(
            Guid.NewGuid(), title, null, 100m,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7),
            categoryId, null);

    private static AuctionEntity CreateEndedAuction(string title)
    {
        var a = CreateActiveAuction(title);
        a.End(null, null);
        return a;
    }

    private async Task SeedAuctionsAsync(params AuctionEntity[] auctions)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        db.Auctions.AddRange(auctions);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAuctions_NoFilter_Returns200WithPaginatedResults()
    {
        var a1 = CreateActiveAuction("Integration Test Watch A");
        var a2 = CreateActiveAuction("Integration Test Ring A");
        await SeedAuctionsAsync(a1, a2);

        var response = await fixture.Client.GetAsync("/api/auctions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedContainer>>();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetAuctions_WithKeyword_FiltersCorrectly()
    {
        var unique = $"UniqueKeyword{Guid.NewGuid():N}";
        var a = CreateActiveAuction($"Item {unique} Product");
        await SeedAuctionsAsync(a);

        var response = await fixture.Client.GetAsync($"/api/auctions?q={unique.ToLower()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedContainer>>();
        body!.Data!.Items.Should().NotBeEmpty();
        body.Data.Items.Should().Contain(i => i.Title.Contains(unique));
    }

    [Fact]
    public async Task GetAuctions_WithCategoryId_FiltersByCategory()
    {
        var catId = IntegrationTestFixture.CategoryElectronicsId;
        var a = CreateActiveAuction($"CategoryFilterTest_{Guid.NewGuid():N}", catId);
        await SeedAuctionsAsync(a);

        var response = await fixture.Client.GetAsync($"/api/auctions?categoryId={catId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedContainer>>();
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAuctions_WithPaging_ReturnsPaginatedSubset()
    {
        var response = await fixture.Client.GetAsync("/api/auctions?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedContainer>>();
        body!.Success.Should().BeTrue();
        body.Data!.PageSize.Should().Be(2);
        body.Data.Items.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAuctions_PageSizeOver100_Returns422()
    {
        var response = await fixture.Client.GetAsync("/api/auctions?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetAuctions_EndedAuctions_DoNotAppearInResults()
    {
        var uniqueTitle = $"EndedItem_{Guid.NewGuid():N}";
        var ended = CreateEndedAuction(uniqueTitle);
        await SeedAuctionsAsync(ended);

        var response = await fixture.Client.GetAsync($"/api/auctions?q={uniqueTitle}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedContainer>>();
        body!.Data!.Items.Should().NotContain(i => i.Title == uniqueTitle);
    }

    // Helper classes for deserialization
    private class PagedContainer
    {
        public List<AuctionItemResult> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    private class AuctionItemResult
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public Guid? CategoryId { get; init; }
    }
}
