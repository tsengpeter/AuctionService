using MemberService.Domain.Exceptions;

namespace MemberService.Domain.ValueObjects;

/// <summary>
/// Username value object
/// </summary>
public class Username : IEquatable<Username>
{
    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Username instance
    /// </summary>
    public static Result<Username> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Username>.Failure("使用者名稱為必填欄位");

        if (value.Length < 3)
            return Result<Username>.Failure("使用者名稱長度至少 3 個字元");

        if (value.Length > 50)
            return Result<Username>.Failure("使用者名稱長度不可超過 50 字元");

        if (!IsValidUsernameFormat(value))
            return Result<Username>.Failure("Username can only contain letters and spaces");

        return Result<Username>.Success(new Username(value.Trim()));
    }

    private static bool IsValidUsernameFormat(string username)
    {
        return username.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
    }

    public bool Equals(Username? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Username);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(Username left, Username right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Username left, Username right)
    {
        return !left.Equals(right);
    }
}