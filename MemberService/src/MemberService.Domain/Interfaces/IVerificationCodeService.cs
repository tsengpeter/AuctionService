using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Interfaces;

/// <summary>
/// Interface for managing verification codes stored in Redis
/// </summary>
public interface IVerificationCodeService
{
    /// <summary>
    /// Generate and store a new verification code in Redis
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="verificationType">Type of verification (EmailVerification, PhoneVerification)</param>
    /// <param name="deliveryMethod">Delivery method (Email, Sms)</param>
    /// <param name="target">Target (email address or phone number in E.164 format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated 6-digit verification code</returns>
    Task<string> GenerateAndStoreAsync(long userId, string verificationType, string deliveryMethod, string target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve verification code from Redis
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="verificationType">Type of verification</param>
    /// <param name="deliveryMethod">Delivery method</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification code object or null if not found</returns>
    Task<VerificationCode?> GetAsync(long userId, string verificationType, string deliveryMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete verification code from Redis
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="verificationType">Type of verification</param>
    /// <param name="deliveryMethod">Delivery method</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(long userId, string verificationType, string deliveryMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update verification code attempt count in Redis
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="verificationType">Type of verification</param>
    /// <param name="deliveryMethod">Delivery method</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated verification code or null if not found</returns>
    Task<VerificationCode?> IncrementAttemptAsync(long userId, string verificationType, string deliveryMethod, CancellationToken cancellationToken = default);
}
