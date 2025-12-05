namespace MemberService.Domain.Exceptions;

/// <summary>
/// Test domain exception for testing purposes
/// </summary>
public class TestDomainException : DomainException
{
    public TestDomainException(string message) : base(message)
    {
    }
}