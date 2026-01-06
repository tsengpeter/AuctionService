namespace BiddingService.Core.DTOs.Responses;

/// <summary>
/// Represents the result of JWT token validation from Member Service.
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Indicates whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The user ID if the token is valid, null otherwise.
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// The token expiration date/time if the token is valid, null otherwise.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Error message if the token is invalid, null otherwise.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static TokenValidationResult Success(long userId, DateTime? expiresAt = null)
    {
        return new TokenValidationResult
        {
            IsValid = true,
            UserId = userId,
            ExpiresAt = expiresAt,
            ErrorMessage = null
        };
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static TokenValidationResult Failure(string? errorMessage = null)
    {
        return new TokenValidationResult
        {
            IsValid = false,
            UserId = null,
            ExpiresAt = null,
            ErrorMessage = errorMessage ?? "Invalid or expired token."
        };
    }
}