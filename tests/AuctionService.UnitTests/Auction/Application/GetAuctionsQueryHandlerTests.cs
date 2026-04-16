using Auction.Application.DTOs;
using Auction.Application.Queries.GetAuctions;
using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.UnitTests.Auction.Application;

public class GetAuctionsQueryHandlerTests : IAsyncLifetime
{
    private AuctionDbContext _db = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AuctionDbContext(options);
        await SeedTestData();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task SeedTestData()
    {
        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();

        var auctions = new[]
        {
            CreateAuction("Vintage Watch", 100m, cat1, AuctionStatus.Active),
            CreateAuction("Silver Ring", 50m, cat1, AuctionStatus.Active),
            CreateAuction("Luxury Car", 5000m, cat2, AuctionStatus.Active),
            CreateAuction("Old Painting", 200m, cat2, AuctionStatus.Active),
            CreateAuction("Ended Necklace", 75m, cat1, AuctionStatus.Ended),
        };

        _db.Auctions.AddRange(auctions);
        await _db.SaveChangesAsync();

        // Store well-known IDs for filtering tests
        _cat1 = cat1;
        _cat2 = cat2;
    }

    private Guid _cat1;
    private Guid _cat2;

    private static global::Auction.Domain.Entities.Auction CreateAuction(
        string title, decimal price, Guid catId, AuctionStatus status)
    {
        var auction = global::Auction.Domain.Entities.Auction.Create(
            Guid.NewGuid(), title, null, price,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7),
            catId, null);
        if (status == AuctionStatus.Ended)
            auction.End(null, null);
        return auction;
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsPaged_ActiveAuctions()
    {
        var handler = new GetAuctionsQueryHandler(_db);
        var query = new GetAuctionsQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(4); // 4 active, 1 ended
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WithKeyword_FiltersActiveAuctions()
    {
        var handler = new GetAuctionsQueryHandler(_db);
        var query = new GetAuctionsQuery { Q = "watch" }; // "Vintage Watch" contains "watch"

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("Vintage Watch");
    }

    [Fact]
    public async Task Handle_WithCategoryId_FiltersByCategory()
    {
        var handler = new GetAuctionsQueryHandler(_db);
        var query = new GetAuctionsQuery { CategoryId = _cat2 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2); // Luxury Car + Old Painting
        result.Items.All(a => a.CategoryId == _cat2).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNoResults_ReturnsEmptyPaged()
    {
        var handler = new GetAuctionsQueryHandler(_db);
        var query = new GetAuctionsQuery { Q = "nonexistentXXXYYY" };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_EndedAuctions_DoNotAppearInResults()
    {
        var handler = new GetAuctionsQueryHandler(_db);
        var query = new GetAuctionsQuery { Q = "Necklace" }; // Only "Ended Necklace" matches

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty(); // Ended auction excluded
    }

    [Fact]
    public async Task Handle_PageSizeOver100_ThrowsValidationException()
    {
        var validator = new GetAuctionsQueryValidator();
        var query = new GetAuctionsQuery { PageSize = 101 };

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuctionsQuery.PageSize));
    }
}
