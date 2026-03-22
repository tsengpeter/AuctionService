using Auction;
using Auction.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.UnitTests.Auction;

public class AuctionDependencyInjectionTests
{
    [Fact]
    public void AddAuctionModule_ShouldRegisterAuctionDbContext()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddAuctionModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<AuctionDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void AddAuctionModule_ShouldRegisterMediatR()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddAuctionModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddAuctionModule_WhenNoConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var act = () => services.AddAuctionModule(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Auction module*");
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();
}
