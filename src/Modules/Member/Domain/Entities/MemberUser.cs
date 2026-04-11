using AuctionService.Shared;
using Member.Domain;

namespace Member.Domain.Entities;

public class MemberUser : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string UsernameNormalized { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public MemberRole Role { get; private set; } = MemberRole.Member;

    public string? AddressCountry { get; private set; }
    public string? AddressCity { get; private set; }
    public string? AddressPostalCode { get; private set; }
    public string? AddressLine { get; private set; }

    public int PhoneCountryCodeId { get; private set; }
    public PhoneCountryCode? CountryCode { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;

    private MemberUser() { }

    public static MemberUser Create(
        string email,
        string username,
        string passwordHash,
        int phoneCountryCodeId,
        string phoneNumber,
        string? displayName = null,
        string? addressCountry = null,
        string? addressCity = null,
        string? addressPostalCode = null,
        string? addressLine = null)
    {
        var now = DateTimeOffset.UtcNow;
        var trimmedUsername = username.Trim();
        return new MemberUser
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            Username = trimmedUsername,
            UsernameNormalized = trimmedUsername.ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? trimmedUsername : displayName.Trim(),
            Role = MemberRole.Member,
            AddressCountry = addressCountry?.Trim(),
            AddressCity = addressCity?.Trim(),
            AddressPostalCode = addressPostalCode?.Trim(),
            AddressLine = addressLine?.Trim(),
            PhoneCountryCodeId = phoneCountryCodeId,
            PhoneNumber = phoneNumber.StartsWith("09") ? phoneNumber[1..] : phoneNumber,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string username,
        string? displayName,
        string? addressCountry,
        string? addressCity,
        string? addressPostalCode,
        string? addressLine)
    {
        var trimmedUsername = username.Trim();
        Username = trimmedUsername;
        UsernameNormalized = trimmedUsername.ToLowerInvariant();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? trimmedUsername : displayName.Trim();
        AddressCountry = addressCountry?.Trim();
        AddressCity = addressCity?.Trim();
        AddressPostalCode = addressPostalCode?.Trim();
        AddressLine = addressLine?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
