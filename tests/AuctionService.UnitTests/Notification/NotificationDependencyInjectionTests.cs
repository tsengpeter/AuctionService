using FluentAssertions;
using MediatR;
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
        var config = BuildConfig();

        services.AddNotificationModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<NotificationDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void AddNotificationModule_ShouldRegisterMediatR()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddNotificationModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddNotificationModule_WhenNoConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var act = () => services.AddNotificationModule(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Notification module*");
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();
}
