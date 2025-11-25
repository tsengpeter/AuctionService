namespace MemberService.Application.DTOs.Users;

/// <summary>
/// User profile response DTO for authenticated users viewing their own complete profile
/// </summary>
public class UserProfileResponse
{
    /// <summary>
    /// User ID (Snowflake format)
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Account creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last profile update timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
