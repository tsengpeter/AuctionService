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

        if (!HasUpperCase(value))
            return Result<Password>.Failure("密碼必須包含至少一個大寫字母");

        if (!HasLowerCase(value))
            return Result<Password>.Failure("密碼必須包含至少一個小寫字母");

        if (!HasDigit(value))
            return Result<Password>.Failure("密碼必須包含至少一個數字");

        if (!HasSpecialChar(value))
            return Result<Password>.Failure("密碼必須包含至少一個特殊符號");

        return Result<Password>.Success(new Password(value));
    }

    private static bool HasUpperCase(string password) => password.Any(char.IsUpper);
    private static bool HasLowerCase(string password) => password.Any(char.IsLower);
    private static bool HasDigit(string password) => password.Any(char.IsDigit);
    private static bool HasSpecialChar(string password) => password.Any(c => !char.IsLetterOrDigit(c));

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