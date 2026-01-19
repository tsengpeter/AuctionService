namespace MemberService.Domain.Interfaces;

/// <summary>
/// SMS sending service interface
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Send verification code via SMS
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number (with country code)</param>
    /// <param name="code">6-digit verification code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
