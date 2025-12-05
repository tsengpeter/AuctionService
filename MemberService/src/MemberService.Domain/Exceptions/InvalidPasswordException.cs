namespace MemberService.Domain.Exceptions;

public class InvalidPasswordException : DomainException
{
    public InvalidPasswordException() 
        : base("The provided old password is incorrect.")
    {
    }
}