using MediatR;

namespace AuctionService.Shared.Events;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
