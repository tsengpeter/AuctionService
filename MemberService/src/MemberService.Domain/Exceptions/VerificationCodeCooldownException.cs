namespace MemberService.Domain.Exceptions;

/// <summary>
/// Exception thrown when verification code is in cooldown period
/// </summary>
public class VerificationCodeCooldownException : DomainException
{
    public VerificationCodeCooldownException(int remainingSeconds)
        : base($"Please wait {remainingSeconds} seconds before requesting a new verification code")
    {
        RemainingSeconds = remainingSeconds;
    }

    public int RemainingSeconds { get; set; }
}
