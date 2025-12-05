using MemberService.Domain.Exceptions;

namespace MemberService.Domain.Exceptions;

public class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email)
        : base($"An account with email '{email}' already exists.")
    {
    }
}