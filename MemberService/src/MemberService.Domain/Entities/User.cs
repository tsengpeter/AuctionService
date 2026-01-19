using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Entities;

/// <summary>
/// User entity
/// </summary>
public class User
{
    public long Id { get; private set; }
    public Email Email { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Username Username { get; private set; } = null!;
    public bool EmailVerified { get; private set; } = false;
    public bool PhoneNumberVerified { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation property
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() { } // EF Core constructor

    public User(long id, Email email, string phoneNumber, string passwordHash, Username username)
    {
        Id = id;
        Email = email;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
        Username = username;
        EmailVerified = false;
        PhoneNumberVerified = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateUsername(Username newUsername)
    {
        Username = newUsername;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(Email newEmail)
    {
        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyPhoneNumber()
    {
        PhoneNumberVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }
}