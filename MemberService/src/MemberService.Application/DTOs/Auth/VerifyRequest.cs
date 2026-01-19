namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Verify email verification code request DTO
/// </summary>
public class VerifyEmailRequest
{
    /// <summary>
    /// 6-digit verification code
    /// </summary>
    public required string Code { get; set; }
}

/// <summary>
/// Verify phone verification code request DTO
/// </summary>
public class VerifyPhoneRequest
{
    /// <summary>
    /// 6-digit verification code
    /// </summary>
    public required string Code { get; set; }
}
