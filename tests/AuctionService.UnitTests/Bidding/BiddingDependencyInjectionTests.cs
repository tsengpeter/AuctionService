using Bidding;
using Bidding.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.UnitTests.Bidding;

public class BiddingDependencyInjectionTests
{
    [Fact]
    public void AddBiddingModule_ShouldRegisterBiddingDbContext()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddBiddingModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<BiddingDbContext>();
        db.Should().NotBeNull();
    }
}
