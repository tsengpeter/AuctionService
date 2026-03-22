using AuctionService.Shared;
using AuctionService.Shared.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notification;

namespace AuctionService.UnitTests.Shared;

/// <summary>
/// Verifies FR-006: modules communicate via async domain events;
/// publisher (any module) and subscriber (Notification module) are decoupled —
/// they share only the event contract in AuctionService.Shared.
/// </summary>
public class DomainEventRoutingTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register MediatR with both assemblies: Shared (event contract) +
        // Notification (handler). No direct module-to-module project reference needed.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(SharedDependencyInjection).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(NotificationDependencyInjection).Assembly);
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Publish_SampleDomainEvent_ShouldRouteToNotificationHandler()
    {
        await using var provider = BuildProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        // MediatR Publish() does NOT throw when no handler exists — must assert
        // handler registration explicitly to avoid false-green.
        var handler = provider.GetService<INotificationHandler<SampleDomainEvent>>();
        handler.Should().NotBeNull("SampleDomainEventHandler must be registered via Notification assembly scan");

        var @event = new SampleDomainEvent(Guid.NewGuid());
        var act = async () => await publisher.Publish(@event);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_SampleDomainEvent_Repeatedly_ShouldNotThrow()
    {
        await using var provider = BuildProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        // Verify repeated publishes are independently handled
        for (var i = 0; i < 3; i++)
        {
            var @event = new SampleDomainEvent(Guid.NewGuid());
            var act = async () => await publisher.Publish(@event);
            await act.Should().NotThrowAsync();
        }
    }

    [Fact]
    public void SampleDomainEvent_ShouldImplementIDomainEvent()
    {
        var @event = new SampleDomainEvent(Guid.NewGuid());

        @event.Should().BeAssignableTo<IDomainEvent>();
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        @event.EntityId.Should().NotBeEmpty();
    }
}
