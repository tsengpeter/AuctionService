using FluentAssertions;
using Notification.Domain.Entities;

namespace AuctionService.UnitTests.Notification;

public class NotificationRecordTests
{
    [Fact]
    public void Create_ShouldReturnNotificationRecord_WithNullSentAt()
    {
        var recipientId = Guid.NewGuid();
        var record = NotificationRecord.Create(recipientId, "AuctionWon", "{\"auctionId\":\"123\"}");

        record.RecipientId.Should().Be(recipientId);
        record.Type.Should().Be("AuctionWon");
        record.Payload.Should().Be("{\"auctionId\":\"123\"}");
        record.SentAt.Should().BeNull();
        record.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WhenRecipientIdIsEmpty_ShouldThrow()
    {
        var act = () => NotificationRecord.Create(Guid.Empty, "Type", "payload");
        act.Should().Throw<ArgumentException>().WithParameterName("recipientId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WhenTypeIsNullOrWhitespace_ShouldThrow(string? type)
    {
        var act = () => NotificationRecord.Create(Guid.NewGuid(), type!, "payload");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkSent_ShouldSetSentAt()
    {
        var record = NotificationRecord.Create(Guid.NewGuid(), "TestType", "{}");
        record.MarkSent();
        record.SentAt.Should().NotBeNull();
        record.SentAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
