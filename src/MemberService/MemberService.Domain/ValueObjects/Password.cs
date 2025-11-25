namespace MemberService.Domain.ValueObjects;

/// <summary>
/// Password value object.
/// Represents a plaintext password with validation (not hashed).
/// Note: This is used for validation during password creation/change,
/// the actual storage uses PasswordHash entity field.
/// </summary>
public class Password : IEquatable<Password>
{
    private const int MinLength = 8;
    private const int MaxLength = 128;
    
    public string Value { get; }
    
    private Password(string value)
    {
        Value = value;
    }
    
    /// <summary>
    /// Creates a new Password value object.
    /// </summary>
    /// <param name="value">The plaintext password string</param>
    /// <returns>A new Password value object</returns>
    /// <exception cref="ArgumentException">Thrown when password length is invalid</exception>
    public static Password Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("密碼不能為空", nameof(value));
        
        var trimmedValue = value.Trim();
        
        if (trimmedValue.Length < MinLength)
            throw new ArgumentException($"密碼長度至少 {MinLength} 個字元", nameof(value));
        
        if (trimmedValue.Length > MaxLength)
            throw new ArgumentException($"密碼長度不能超過 {MaxLength} 個字元", nameof(value));
        
        return new Password(trimmedValue);
    }
    
    public bool Equals(Password? other)
    {
        return other is not null && Value == other.Value;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Password password && Equals(password);
    }
    
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    
    public static bool operator ==(Password? left, Password? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }
    
    public static bool operator !=(Password? left, Password? right)
    {
        return !(left == right);
    }
    
    public override string ToString()
    {
        return "***REDACTED***";
    }
}
