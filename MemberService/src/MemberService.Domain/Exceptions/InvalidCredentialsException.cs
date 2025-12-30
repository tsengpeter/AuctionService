using MemberService.Domain.Exceptions;

namespace MemberService.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}