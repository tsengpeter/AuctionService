namespace MemberService.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to use an email that already exists in the system
/// </summary>
public class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string message) 
        : base(message, "DUPLICATE_EMAIL")
    {
    }
}
