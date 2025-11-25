namespace MemberService.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to change password with an incorrect old password.
/// </summary>
public class InvalidOldPasswordException : DomainException
{
    public InvalidOldPasswordException() 
        : base("舊密碼不正確", "INVALID_OLD_PASSWORD")
    {
    }
}
