namespace MemberService.Application.DTOs.Users;

/// <summary>
/// User profile update request DTO
/// Allows updating username and/or email (both fields are optional)
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// New username (optional)
    /// If null or empty, username will not be updated
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// New email address (optional)
    /// If null or empty, email will not be updated
    /// Must be unique across all users if provided
    /// </summary>
    public string? Email { get; set; }
}
