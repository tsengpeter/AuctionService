using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Domain.Entities;
using Notification.Infrastructure.Persistence;

namespace AuctionService.IntegrationTests.Notification;

[Collection("Integration")]
public class NotificationDbContextTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void DbContext_ShouldUseSchema_Notification()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var entity = db.Model.FindEntityType(typeof(NotificationRecord));
        entity!.GetSchema().Should().Be("notification");
    }

    [Fact]
    public async Task DbContext_ShouldCreateAndReadNotification()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var record = NotificationRecord.Create(Guid.NewGuid(), "AuctionEnded", "{\"auctionId\":\"123\"}");

        db.Notifications.Add(record);
        await db.SaveChangesAsync();

        var loaded = await db.Notifications.FindAsync(record.Id);
        loaded.Should().NotBeNull();
        loaded!.Type.Should().Be("AuctionEnded");
    }
}
