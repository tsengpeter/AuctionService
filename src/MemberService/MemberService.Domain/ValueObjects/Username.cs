using System.Text.RegularExpressions;

namespace MemberService.Domain.ValueObjects;

/// <summary>
/// Username value object.
/// Represents a user display name with validation.
/// Only allows letters (including Unicode) and spaces.
/// </summary>
public class Username : IEquatable<Username>
{
    private const int MinLength = 3;
    private const int MaxLength = 50;

    // Regex pattern: only letters (Unicode property \p{L}) and spaces allowed
    private static readonly Regex UsernameRegex = new(
        @"^[\p{L}\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Username value object.
    /// </summary>
    /// <param name="value">The username string (letters and spaces only)</param>
    /// <returns>A new Username value object</returns>
    /// <exception cref="ArgumentException">Thrown when username is invalid</exception>
    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("使用者名稱不能為空", nameof(value));

        var trimmedValue = value.Trim();

        if (trimmedValue.Length < MinLength)
            throw new ArgumentException($"使用者名稱長度至少 {MinLength} 個字元", nameof(value));

        if (trimmedValue.Length > MaxLength)
            throw new ArgumentException($"使用者名稱長度不能超過 {MaxLength} 個字元", nameof(value));

        if (!UsernameRegex.IsMatch(trimmedValue))
            throw new ArgumentException("使用者名稱只能包含字母和空格", nameof(value));

        return new Username(trimmedValue);
    }

    public bool Equals(Username? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Username username && Equals(username);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Username? left, Username? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Username? left, Username? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Value;
    }
}
