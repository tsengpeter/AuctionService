using MemberService.Domain.Exceptions;

namespace MemberService.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(long userId)
        : base($"User with ID '{userId}' was not found.")
    {
    }

    public UserNotFoundException(string identifier)
        : base($"User '{identifier}' was not found.")
    {
    }
}