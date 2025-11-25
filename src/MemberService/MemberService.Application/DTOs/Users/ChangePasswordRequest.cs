namespace MemberService.Application.DTOs.Users;

/// <summary>
/// Password change request DTO
/// Requires both old and new passwords for security
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password for verification
    /// Must match the user's current password hash
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password to set
    /// Must meet password requirements (8-128 characters)
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
