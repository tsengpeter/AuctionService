namespace MemberService.Domain.Exceptions;

/// <summary>
/// Exception thrown when a phone number already exists
/// </summary>
public class PhoneAlreadyExistsException : DomainException
{
    public PhoneAlreadyExistsException(string phoneNumber)
        : base($"Phone number '{phoneNumber}' is already registered")
    {
        PhoneNumber = phoneNumber;
    }

    public string PhoneNumber { get; set; }
}
