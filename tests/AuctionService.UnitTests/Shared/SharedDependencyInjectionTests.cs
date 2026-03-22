using AuctionService.Shared;
using AuctionService.Shared.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.UnitTests.Shared;

public class SharedDependencyInjectionTests
{
    [Fact]
    public void AddSharedServices_ShouldRegisterIMediator()
    {
        var services = new ServiceCollection();

        services.AddSharedServices();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddSharedServices_ShouldRegisterValidationBehavior()
    {
        var services = new ServiceCollection();

        services.AddSharedServices();

        // ValidationBehavior<,> is registered as an open generic IPipelineBehavior
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ValidationBehavior<,>));

        descriptor.Should().NotBeNull("ValidationBehavior<,> should be registered as a pipeline behavior");
    }
}
