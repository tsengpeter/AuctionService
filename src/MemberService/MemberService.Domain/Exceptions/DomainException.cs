namespace MemberService.Domain.Exceptions;

/// <summary>
/// Domain layer base exception.
/// All domain-level exceptions should inherit from this class.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string Code { get; protected set; }

    public DomainException(string message) : base(message) 
    {
        Code = "DOMAIN_ERROR";
    }
    
    public DomainException(string message, string code) : base(message)
    {
        Code = code;
    }
    
    public DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
        Code = "DOMAIN_ERROR";
    }
}
