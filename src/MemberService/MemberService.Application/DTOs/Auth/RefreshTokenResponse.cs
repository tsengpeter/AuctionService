namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Response DTO for token refresh operation.
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// New JWT access token.
    /// </summary>
    public required string AccessToken { get; set; }
}
