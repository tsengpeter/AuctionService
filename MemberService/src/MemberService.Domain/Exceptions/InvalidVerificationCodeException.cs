namespace MemberService.Domain.Exceptions;

/// <summary>
/// Exception thrown when verification code is invalid
/// </summary>
public class InvalidVerificationCodeException : DomainException
{
    public InvalidVerificationCodeException(string message)
        : base(message)
    {
    }

    public static InvalidVerificationCodeException CodeExpired =>
        new("Verification code has expired, please request a new one");

    public static InvalidVerificationCodeException CodeIncorrect(int remainingAttempts) =>
        new($"Verification code is incorrect, {remainingAttempts} attempts remaining");

    public static InvalidVerificationCodeException TooManyAttempts =>
        new("Too many failed verification attempts, please request a new code");

    public static InvalidVerificationCodeException NotFound =>
        new("Verification code not found, please request a new one");
}
