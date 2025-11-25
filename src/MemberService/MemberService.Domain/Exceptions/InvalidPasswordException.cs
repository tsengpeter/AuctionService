namespace MemberService.Domain.Exceptions;

/// <summary>
/// Thrown when password verification fails (incorrect password)
/// </summary>
public class InvalidPasswordException : DomainException
{
    public InvalidPasswordException(string message)
        : base(message, "INVALID_PASSWORD")
    {
    }
}
