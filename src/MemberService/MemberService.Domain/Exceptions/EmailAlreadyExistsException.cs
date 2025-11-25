namespace MemberService.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to register with an email that already exists in the system.
/// </summary>
public class EmailAlreadyExistsException : DomainException
{
    public string Email { get; }

    public EmailAlreadyExistsException(string email)
        : base($"電子郵件 '{email}' 已經被使用", "EMAIL_ALREADY_EXISTS")
    {
        Email = email;
    }
}
