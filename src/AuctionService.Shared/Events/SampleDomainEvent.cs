namespace AuctionService.Shared.Events;

/// <summary>
/// Scaffold demonstration: a cross-module domain event contract.
/// Publisher (any module) raises this event; subscriber modules handle it
/// without any direct project reference to the publisher.
/// Replace with real domain events in feature branches.
/// </summary>
public sealed record SampleDomainEvent(Guid EntityId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
