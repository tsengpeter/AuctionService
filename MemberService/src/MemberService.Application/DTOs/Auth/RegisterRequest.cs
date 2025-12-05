namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Register request DTO
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's password
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// User's username
    /// </summary>
    public required string Username { get; set; }
}