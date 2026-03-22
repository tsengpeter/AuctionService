using AuctionService.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Events;

/// <summary>
/// Scaffold demonstration: cross-module domain event handler.
/// Notification module subscribes to SampleDomainEvent published by any other
/// module without any direct project reference to the publisher.
/// Replace with real notification logic in feature branches.
/// </summary>
public sealed class SampleDomainEventHandler : INotificationHandler<SampleDomainEvent>
{
    private readonly ILogger<SampleDomainEventHandler> _logger;

    public SampleDomainEventHandler(ILogger<SampleDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SampleDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SampleDomainEvent received — EventId: {EventId}, EntityId: {EntityId}, OccurredAt: {OccurredAt}",
            notification.EventId,
            notification.EntityId,
            notification.OccurredAt);

        return Task.CompletedTask;
    }
}
