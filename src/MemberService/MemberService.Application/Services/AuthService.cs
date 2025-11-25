using Microsoft.Extensions.Logging;
using MemberService.Application.DTOs.Auth;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;

namespace MemberService.Application.Services;

/// <summary>
/// Authentication service implementation.
/// Handles user registration, login, token refresh, and logout operations.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISnowflakeIdGenerator _idGenerator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ISnowflakeIdGenerator idGenerator,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        _refreshTokenGenerator = refreshTokenGenerator ?? throw new ArgumentNullException(nameof(refreshTokenGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(string email, string password, string username, CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required", nameof(username));

        // Check if email already exists
        var existingUser = await _userRepository.FindByEmailAsync(email, cancellationToken);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempted with duplicate email: {Email}", email);
            throw new EmailAlreadyExistsException($"Email '{email}' is already registered");
        }

        // Generate snowflake ID
        var userId = _idGenerator.GenerateId();

        // Create value objects
        var emailVo = Email.Create(email);
        var passwordVo = Password.Create(password);
        var usernameVo = Username.Create(username);

        // Hash password with user ID for additional security
        var passwordHash = _passwordHasher.HashPassword(passwordVo.Value, userId);

        // Create user entity
        var user = User.Create(userId, emailVo, usernameVo, passwordHash);

        // Add user to repository
        await _userRepository.AddAsync(user, cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email.Value);
        var refreshTokenString = _refreshTokenGenerator.GenerateToken();
        var refreshToken = RefreshToken.Create(refreshTokenString, user.Id, DateTime.UtcNow.AddDays(7));

        // Add refresh token to repository
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        _logger.LogInformation(
            "Authentication event: User registered | UserId: {UserId} | Email: {Email} | Timestamp: {Timestamp}",
            user.Id,
            email,
            DateTime.UtcNow);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email.Value,
            Username = user.Username.Value,
            AccessToken = accessToken,
            RefreshToken = refreshTokenString
        };
    }

    /// <summary>
    /// Login a user.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        // Find user by email
        var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning(
                "Authentication event: Login failed - Invalid credentials | Email: {Email} | Reason: User not found | Timestamp: {Timestamp}",
                email,
                DateTime.UtcNow);
            throw new InvalidCredentialsException("Invalid email or password");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning(
                "Authentication event: Login failed - Invalid credentials | UserId: {UserId} | Email: {Email} | Reason: Password mismatch | Timestamp: {Timestamp}",
                user.Id,
                email,
                DateTime.UtcNow);
            throw new InvalidCredentialsException("Invalid email or password");
        }

        // Generate tokens
        var accessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email.Value);
        var refreshTokenString = _refreshTokenGenerator.GenerateToken();
        var refreshToken = RefreshToken.Create(refreshTokenString, user.Id, DateTime.UtcNow.AddDays(7));

        // Add refresh token to repository
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        _logger.LogInformation(
            "Authentication event: User logged in | UserId: {UserId} | Email: {Email} | TokenExpiresAt: {TokenExpiresAt} | Timestamp: {Timestamp}",
            user.Id,
            user.Email.Value,
            refreshToken.ExpiresAt,
            DateTime.UtcNow);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email.Value,
            Username = user.Username.Value,
            AccessToken = accessToken,
            RefreshToken = refreshTokenString
        };
    }

    /// <summary>
    /// Refresh an access token using a refresh token.
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token is required", nameof(refreshToken));

        // Find refresh token
        var token = await _refreshTokenRepository.FindByTokenAsync(refreshToken, cancellationToken);
        if (token == null)
        {
            _logger.LogWarning(
                "Authentication event: Token refresh failed | Reason: Token not found | Timestamp: {Timestamp}",
                DateTime.UtcNow);
            throw new InvalidCredentialsException("Invalid refresh token");
        }

        // Verify token is valid
        if (!token.IsValid)
        {
            _logger.LogWarning(
                "Authentication event: Token refresh failed | UserId: {UserId} | Reason: Token expired or revoked | TokenExpiresAt: {TokenExpiresAt} | Timestamp: {Timestamp}",
                token.UserId,
                token.ExpiresAt,
                DateTime.UtcNow);
            throw new InvalidCredentialsException("Refresh token has expired or been revoked");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogError(
                "Authentication event: Token refresh failed | UserId: {UserId} | Reason: User not found | Timestamp: {Timestamp}",
                token.UserId,
                DateTime.UtcNow);
            throw new InvalidCredentialsException("User not found");
        }

        // Generate new access token
        var accessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email.Value);

        _logger.LogInformation(
            "Authentication event: Token refreshed | UserId: {UserId} | Email: {Email} | Timestamp: {Timestamp}",
            user.Id,
            user.Email.Value,
            DateTime.UtcNow);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email.Value,
            Username = user.Username.Value,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    /// <summary>
    /// Logout a user by revoking their refresh token.
    /// </summary>
    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token is required", nameof(refreshToken));

        // Find refresh token
        var token = await _refreshTokenRepository.FindByTokenAsync(refreshToken, cancellationToken);
        if (token != null)
        {
            // Revoke token
            token.Revoke();

            _logger.LogInformation(
                "Authentication event: User logged out | UserId: {UserId} | TokenRevoked: true | Timestamp: {Timestamp}",
                token.UserId,
                DateTime.UtcNow);
        }
        else
        {
            // If token not found, just log and return (idempotent operation)
            _logger.LogDebug(
                "Authentication event: Logout request with non-existent token | Timestamp: {Timestamp}",
                DateTime.UtcNow);
        }
    }
}
