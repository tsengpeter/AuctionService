using Bidding;
using Bidding.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.UnitTests.Bidding;

public class BiddingDependencyInjectionTests
{
    [Fact]
    public void AddBiddingModule_ShouldRegisterBiddingDbContext()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddBiddingModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<BiddingDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void AddBiddingModule_ShouldRegisterMediatR()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddBiddingModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddBiddingModule_WhenNoConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var act = () => services.AddBiddingModule(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bidding module*");
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();
}
