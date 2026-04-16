using Auction.Infrastructure.Persistence;
using AuctionService.IntegrationTests.Infrastructure;
using AuctionService.IntegrationTests.Member;
using AuctionService.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuctionService.IntegrationTests.Auction;

[Collection("Integration")]
public class CreateAuctionIntegrationTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    private static object ValidRequestBody(string title = "Integration Create Test") => new
    {
        title,
        description = (string?)null,
        startingPrice = 100m,
        startTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
        endTime = DateTimeOffset.UtcNow.AddDays(7).ToString("O"),
        categoryId = (Guid?)null,
        imageUrls = (List<string>?)null
    };

    [Fact]
    public async Task PostAuction_WithoutAuth_Returns401()
    {
        Client.DefaultRequestHeaders.Authorization = null;

        var response = await Client.PostAsJsonAsync("/api/auctions", ValidRequestBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostAuction_ValidRequest_Returns201WithLocation()
    {
        var token = await RegisterAndLoginAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

        var response = await Client.PostAsJsonAsync("/api/auctions", ValidRequestBody($"Create_{Guid.NewGuid():N}"));
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostAuction_InvalidData_Returns422()
    {
        var token = await RegisterAndLoginAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

        var badRequest = new
        {
            title = "",
            startingPrice = -10m,
            startTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            endTime = DateTimeOffset.UtcNow.AddDays(7).ToString("O")
        };
        var response = await Client.PostAsJsonAsync("/api/auctions", badRequest);
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostAuction_CreatedAuction_AppearsInGetAuctions()
    {
        var token = await RegisterAndLoginAsync();
        var uniqueTitle = $"Appear_{Guid.NewGuid():N}";
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

        var createResponse = await Client.PostAsJsonAsync("/api/auctions", ValidRequestBody(uniqueTitle));
        Client.DefaultRequestHeaders.Authorization = null;

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await Client.GetAsync($"/api/auctions?q={uniqueTitle}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedContainer>>();
        body!.Data!.Items.Should().NotBeEmpty();
        body.Data.Items.Should().Contain(i => i.Title.Contains(uniqueTitle));
    }

    private class PagedContainer
    {
        public List<AuctionItem> Items { get; init; } = [];
    }

    private class AuctionItem
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
    }
}
