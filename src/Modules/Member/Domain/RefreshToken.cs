using AuctionService.Shared;

namespace Member.Domain;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
}
