namespace MemberService.Domain.Exceptions;

/// <summary>
/// Thrown when login credentials (email/password) are invalid.
/// </summary>
public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() 
        : base("電子郵件或密碼不正確", "INVALID_CREDENTIALS")
    {
    }

    public InvalidCredentialsException(string message)
        : base(message, "INVALID_CREDENTIALS")
    {
    }
}
