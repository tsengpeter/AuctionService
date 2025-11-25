using MemberService.Application.DTOs.Auth;

namespace MemberService.Application.Services;

/// <summary>
/// Authentication service interface for user registration, login, token refresh, and logout operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user with email, password, and username.
    /// </summary>
    Task<AuthResponse> RegisterAsync(string email, string password, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Login a user with email and password.
    /// </summary>
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh an access token using a refresh token.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout a user by revoking their refresh token.
    /// </summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
