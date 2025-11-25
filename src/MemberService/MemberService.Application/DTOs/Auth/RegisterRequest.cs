namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Request DTO for user registration.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's password (8-128 characters).
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// User's username (3-50 characters, letters and spaces only).
    /// </summary>
    public required string Username { get; set; }
}
