using MemberService.Domain.Exceptions;

namespace MemberService.Domain.Exceptions;

public class RefreshTokenExpiredException : DomainException
{
    public RefreshTokenExpiredException()
        : base("Refresh token has expired.")
    {
    }
}