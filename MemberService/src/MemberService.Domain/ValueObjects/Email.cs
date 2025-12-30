using MemberService.Domain.Exceptions;

namespace MemberService.Domain.ValueObjects;

/// <summary>
/// Email value object
/// </summary>
public class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Email instance
    /// </summary>
    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("電子郵件為必填欄位");

        if (value.Length > 255)
            return Result<Email>.Failure("電子郵件長度不可超過 255 字元");

        if (!IsValidEmailFormat(value))
            return Result<Email>.Failure("請提供有效的電子郵件地址");

        return Result<Email>.Success(new Email(value.ToLowerInvariant()));
    }

    private static bool IsValidEmailFormat(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(Email? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Email);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(Email left, Email right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Email left, Email right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Result type for domain operations
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null);
    }

    public static Result<T> Failure(string error)
    {
        return new Result<T>(false, default, error);
    }
}