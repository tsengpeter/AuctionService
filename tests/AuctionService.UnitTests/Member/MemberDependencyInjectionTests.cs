using FluentAssertions;
using MediatR;
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
        var config = BuildConfig();

        services.AddMemberModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<MemberDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void AddMemberModule_ShouldRegisterMediatR()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddMemberModule(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddMemberModule_WhenNoConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var act = () => services.AddMemberModule(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Member module*");
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();
}
