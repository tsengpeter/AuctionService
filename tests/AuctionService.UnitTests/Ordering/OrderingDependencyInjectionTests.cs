using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering;
using Ordering.Infrastructure.Persistence;

namespace AuctionService.UnitTests.Ordering;

public class OrderingDependencyInjectionTests
{
    [Fact]
    public void AddOrderingModule_ShouldRegisterOrderingDbContext()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddOrderingModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<OrderingDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void AddOrderingModule_ShouldRegisterMediatR()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddOrderingModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddOrderingModule_WhenNoConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var act = () => services.AddOrderingModule(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Ordering module*");
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();
}
