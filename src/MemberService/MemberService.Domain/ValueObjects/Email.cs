using System.Text.RegularExpressions;

namespace MemberService.Domain.ValueObjects;

/// <summary>
/// Email value object.
/// Represents an email address with validation and normalization.
/// </summary>
public class Email : IEquatable<Email>
{
    private const int MaxLength = 255;
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private Email(string value)
    {
        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Creates a new Email value object.
    /// </summary>
    /// <param name="value">The email address string</param>
    /// <returns>A new Email value object</returns>
    /// <exception cref="ArgumentException">Thrown when email is invalid or exceeds max length</exception>
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("電子郵件地址不能為空", nameof(value));

        var trimmedValue = value.Trim();

        if (trimmedValue.Length > MaxLength)
            throw new ArgumentException($"電子郵件地址不能超過 {MaxLength} 個字元", nameof(value));

        if (!EmailRegex.IsMatch(trimmedValue))
            throw new ArgumentException("電子郵件地址格式無效", nameof(value));

        return new Email(trimmedValue);
    }

    public bool Equals(Email? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Email email && Equals(email);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Email? left, Email? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Email? left, Email? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Value;
    }
}
