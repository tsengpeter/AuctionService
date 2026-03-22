using AuctionService.Shared;

namespace Member.Domain.Entities;

public class MemberUser : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    private MemberUser() { }

    public static MemberUser Create(string email, string username, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new MemberUser
        {
            Email = email.Trim().ToLowerInvariant(),
            Username = username.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateTimestamp() => UpdatedAt = DateTimeOffset.UtcNow;
}
