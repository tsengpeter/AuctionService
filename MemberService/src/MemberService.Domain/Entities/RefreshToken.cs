namespace MemberService.Domain.Entities;

/// <summary>
/// RefreshToken entity
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = null!;
    public long UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private RefreshToken() { } // EF Core constructor

    public RefreshToken(Guid id, string token, long userId, DateTime expiresAt)
    {
        Id = id;
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        IsRevoked = true;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsRevoked && !IsExpired;
}