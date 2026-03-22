using FluentAssertions;
using Member;
using Member.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.UnitTests.Member;

public class MemberDependencyInjectionTests
{
    [Fact]
    public void AddMemberModule_ShouldRegisterMemberDbContext()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddMemberModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<MemberDbContext>();
        db.Should().NotBeNull();
    }
}
