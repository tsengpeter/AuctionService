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
    Task<RefreshTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout a user by revoking their refresh token.
    /// </summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Authentication response containing user information and tokens.
/// </summary>
public class AuthResponse
{
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Response for token refresh containing new access token.
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
}
