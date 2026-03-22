using FluentAssertions;
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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddOrderingModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<OrderingDbContext>();
        db.Should().NotBeNull();
    }
}
