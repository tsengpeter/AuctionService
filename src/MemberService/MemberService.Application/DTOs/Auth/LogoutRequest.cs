namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Request DTO for logout operation.
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// The refresh token to revoke.
    /// </summary>
    public required string RefreshToken { get; set; }
}
