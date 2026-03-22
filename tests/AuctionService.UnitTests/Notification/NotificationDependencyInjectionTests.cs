using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification;
using Notification.Infrastructure.Persistence;

namespace AuctionService.UnitTests.Notification;

public class NotificationDependencyInjectionTests
{
    [Fact]
    public void AddNotificationModule_ShouldRegisterNotificationDbContext()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddNotificationModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<NotificationDbContext>();
        db.Should().NotBeNull();
    }
}
