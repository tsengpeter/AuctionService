namespace MemberService.Domain.ValueObjects;

/// <summary>
/// Verification code value object
/// Represents a 6-digit verification code stored in Redis
/// </summary>
public class VerificationCode
{
    /// <summary>
    /// 6-digit numeric verification code
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Number of failed verification attempts (0-3)
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Verification code creation time (UTC)
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Verification target (email address or phone number in E.164 format)
    /// </summary>
    public string Target { get; private set; }

    public VerificationCode(string code, string target)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit))
        {
            throw new ArgumentException("Verification code must be exactly 6 digits", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Target cannot be empty", nameof(target));
        }

        Code = code;
        Target = target;
        AttemptCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increment the attempt count
    /// </summary>
    public void IncrementAttempt()
    {
        if (AttemptCount < 3)
        {
            AttemptCount++;
        }
    }

    /// <summary>
    /// Check if the code has exceeded maximum attempts
    /// </summary>
    public bool IsMaxAttemptsExceeded => AttemptCount >= 3;

    /// <summary>
    /// Check if the code has expired (5 minutes)
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > CreatedAt.AddMinutes(5);

    /// <summary>
    /// Check if the code can be resent (60-second cooldown)
    /// </summary>
    public bool CanResend => DateTime.UtcNow >= CreatedAt.AddSeconds(60);

    /// <summary>
    /// Get remaining time before expiration in seconds
    /// </summary>
    public int SecondsUntilExpiration
    {
        get
        {
            var expirationTime = CreatedAt.AddMinutes(5);
            var remaining = (int)Math.Round((expirationTime - DateTime.UtcNow).TotalSeconds);
            return Math.Max(0, remaining);
        }
    }

    /// <summary>
    /// Get remaining cooldown time in seconds
    /// </summary>
    public int SecondsUntilCanResend
    {
        get
        {
            var resendTime = CreatedAt.AddSeconds(60);
            var remaining = (int)Math.Round((resendTime - DateTime.UtcNow).TotalSeconds);
            return Math.Max(0, remaining);
        }
    }
}
