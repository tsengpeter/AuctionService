namespace MemberService.Application.DTOs.Users;

/// <summary>
/// Public user profile response DTO - limited information for non-authenticated or other users
/// Only contains public-facing information (username and created date)
/// </summary>
public class PublicUserProfileResponse
{
    /// <summary>
    /// User ID (Snowflake format)
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// User's display name (public information)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Account creation timestamp (UTC) - public information
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
