namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Response DTO for authentication operations (register/login).
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public required long UserId { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// JWT access token for API requests.
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; set; }
}
