namespace MemberService.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to access a user that does not exist.
/// </summary>
public class UserNotFoundException : DomainException
{
    public long? UserId { get; }
    public string? Email { get; }

    public UserNotFoundException(long userId)
        : base($"找不到 ID 為 {userId} 的使用者", "USER_NOT_FOUND")
    {
        UserId = userId;
    }

    public UserNotFoundException(string email)
        : base($"找不到電子郵件為 '{email}' 的使用者", "USER_NOT_FOUND")
    {
        Email = email;
    }
}
