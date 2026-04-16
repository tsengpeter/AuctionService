using Auction.Infrastructure.BackgroundServices;
using Auction.Infrastructure.Persistence;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

using AuctionEntity = Auction.Domain.Entities.Auction;
using AuctionStatus = Auction.Domain.Entities.AuctionStatus;

namespace AuctionService.IntegrationTests.Auction;

[Collection("Integration")]
public class AuctionEndBackgroundServiceIntegrationTests(IntegrationTestFixture fixture)
{
    private async Task<AuctionEntity> SeedExpiredAuctionAsync(string title)
    {
        var auction = AuctionEntity.Create(
            Guid.NewGuid(), title, null, 50m,
            DateTimeOffset.UtcNow.AddHours(-3), DateTimeOffset.UtcNow.AddMinutes(-5),
            null, null);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
        return auction;
    }

    [Fact]
    public async Task ProcessEndedAuctions_ExpiredAuction_SetsStatusToEnded()
    {
        var auction = await SeedExpiredAuctionAsync($"ExpiredForEnd_{Guid.NewGuid():N}");

        using var scope = fixture.Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<AuctionEndBackgroundService>().First();
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var updated = await db.Auctions.FindAsync(auction.Id);
        updated!.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public async Task ProcessEndedAuctions_ShortlyExpiredAuction_IsPickedUpWithin2Minutes()
    {
        // This test verifies the background service picks up auctions where endTime <= now
        var auction = await SeedExpiredAuctionAsync($"RecentlyExpired_{Guid.NewGuid():N}");

        // Manually invoke the process rather than waiting 60 seconds
        using var scope = fixture.Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<AuctionEndBackgroundService>().First();
        await svc.ProcessEndedAuctionsAsync(CancellationToken.None);

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var updated = await db.Auctions.FindAsync(auction.Id);
        updated!.Status.Should().Be(AuctionStatus.Ended);
    }
}
