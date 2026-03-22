using AuctionService.Shared;

namespace Notification.Domain.Entities;

public class NotificationRecord : BaseEntity
{
    public Guid RecipientId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset? SentAt { get; private set; }

    private NotificationRecord() { }

    public static NotificationRecord Create(Guid recipientId, string type, string payload)
    {
        if (recipientId == Guid.Empty) throw new ArgumentException("RecipientId cannot be empty.", nameof(recipientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var now = DateTimeOffset.UtcNow;
        return new NotificationRecord
        {
            RecipientId = recipientId,
            Type = type.Trim(),
            Payload = payload,
            SentAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkSent()
    {
        SentAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
