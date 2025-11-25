namespace MemberService.Domain.Entities;

/// <summary>
/// RefreshToken entity.
/// Represents a refresh token that can be used to obtain a new access token.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; }
    public long UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    /// <summary>
    /// Computed property: indicates whether the token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Computed property: indicates whether the token is still valid for use.
    /// Valid means: not expired AND not revoked.
    /// </summary>
    public bool IsValid => !IsExpired && !IsRevoked;

    // Constructor for EF Core
    private RefreshToken()
    {
        Token = null!;
    }

    /// <summary>
    /// Creates a new RefreshToken entity.
    /// </summary>
    /// <param name="token">The token value (JWT-like string)</param>
    /// <param name="userId">The associated user ID</param>
    /// <param name="expiresAt">The expiration time in UTC</param>
    /// <returns>A new RefreshToken entity</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public static RefreshToken Create(string token, long userId, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token 不能為空", nameof(token));

        if (userId <= 0)
            throw new ArgumentException("使用者ID必須大於0", nameof(userId));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Revokes the refresh token, preventing it from being used.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }
}
