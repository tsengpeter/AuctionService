namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Request DTO for token refresh.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token string.
    /// </summary>
    public required string RefreshToken { get; set; }
}
