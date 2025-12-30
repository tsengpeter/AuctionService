using MemberService.Domain.Exceptions;

namespace MemberService.Domain.Exceptions;

public class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("Invalid refresh token.")
    {
    }
}