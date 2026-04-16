using Auction.Application.Commands.CreateAuction;
using Auction.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace AuctionService.UnitTests.Auction.Application;

public class CreateAuctionCommandHandlerTests : IAsyncLifetime
{
    private AuctionDbContext _db = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AuctionDbContext(options);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private static CreateAuctionCommand ValidCommand(
        decimal startingPrice = 100m,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        List<string>? imageUrls = null) => new(
            OwnerId: Guid.NewGuid(),
            Title: "My Auction",
            Description: null,
            StartingPrice: startingPrice,
            StartTime: startTime ?? DateTimeOffset.UtcNow.AddHours(1),
            EndTime: endTime ?? DateTimeOffset.UtcNow.AddDays(7),
            CategoryId: null,
            ImageUrls: imageUrls);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewAuctionId()
    {
        var handler = new CreateAuctionCommandHandler(_db);
        var command = ValidCommand();

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
        var saved = await _db.Auctions.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("My Auction");
    }

    [Fact]
    public async Task Handle_ZeroStartingPrice_ThrowsValidationException()
    {
        var validator = new CreateAuctionCommandValidator();
        var command = ValidCommand(startingPrice: 0m);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartingPrice");
    }

    [Fact]
    public async Task Handle_StartTimeInPast_ThrowsValidationException()
    {
        var validator = new CreateAuctionCommandValidator();
        var command = ValidCommand(startTime: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartTime");
    }

    [Fact]
    public async Task Handle_EndTimeNotAfterStartTimePlus1Min_ThrowsValidationException()
    {
        var validator = new CreateAuctionCommandValidator();
        var startTime = DateTimeOffset.UtcNow.AddHours(1);
        var command = ValidCommand(startTime: startTime, endTime: startTime.AddSeconds(30));

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndTime");
    }

    [Fact]
    public async Task Handle_TooManyImageUrls_ThrowsValidationException()
    {
        var validator = new CreateAuctionCommandValidator();
        var urls = Enumerable.Range(1, 6).Select(i => $"https://example.com/img{i}.jpg").ToList();
        var command = ValidCommand(imageUrls: urls);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrls");
    }
}
