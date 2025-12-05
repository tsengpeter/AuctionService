using MemberService.Domain.Exceptions;

namespace MemberService.Domain.ValueObjects;

/// <summary>
/// Password value object
/// </summary>
public class Password : IEquatable<Password>
{
    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Password instance
    /// </summary>
    public static Result<Password> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Password>.Failure("密碼為必填欄位");

        if (value.Length < 8)
            return Result<Password>.Failure("密碼長度至少 8 個字元");

        if (value.Length > 128)
            return Result<Password>.Failure("密碼長度不可超過 128 字元");

        return Result<Password>.Success(new Password(value));
    }

    public bool Equals(Password? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Password);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return new string('*', Value.Length);
    }

    public static bool operator ==(Password left, Password right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Password left, Password right)
    {
        return !left.Equals(right);
    }
}