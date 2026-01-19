namespace MemberService.Domain.Interfaces;

/// <summary>
/// Email sending service interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send verification code to email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="code">6-digit verification code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendVerificationCodeAsync(string toEmail, string code, CancellationToken cancellationToken = default);
}
