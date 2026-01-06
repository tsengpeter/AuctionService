using MemberService.Application.DTOs.Auth;
using MemberService.Application.Services;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;

namespace MemberService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIdGenerator _idGenerator;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IIdGenerator idGenerator)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _idGenerator = idGenerator;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        var emailResult = Email.Create(request.Email);
        if (!emailResult.IsSuccess)
        {
            throw new ArgumentException(emailResult.Error);
        }
        var email = emailResult.Value!;

        if (await _userRepository.ExistsByEmailAsync(email))
        {
            throw new EmailAlreadyExistsException(request.Email);
        }

        // Create new user
        var userId = _idGenerator.GenerateId();
        var passwordHash = _passwordHasher.HashPassword(request.Password, userId);

        var usernameResult = Username.Create(request.Username);
        if (!usernameResult.IsSuccess)
        {
            throw new ArgumentException(usernameResult.Error);
        }
        var username = usernameResult.Value!;

        var user = new User(userId, email, passwordHash, username);
        await _userRepository.AddAsync(user);

        // Registration successful - user needs to login separately
        return new RegisterResponse(
            User: new UserInfo(
                Id: user.Id,
                Email: user.Email.Value,
                Username: user.Username.Value,
                CreatedAt: user.CreatedAt
            )
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var emailResult = Email.Create(request.Email);
        if (!emailResult.IsSuccess)
        {
            throw new InvalidCredentialsException();
        }
        var email = emailResult.Value!;

        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.Id, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        // Generate tokens
        var accessToken = _tokenGenerator.GenerateAccessToken(user.Id, user.Email.Value);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        // Store refresh token
        var refreshTokenEntity = new RefreshToken(
            Guid.NewGuid(),
            refreshToken,
            user.Id,
            DateTime.UtcNow.AddDays(7) // Refresh tokens typically last 7 days
        );
        await _refreshTokenRepository.AddAsync(refreshTokenEntity);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1), // JWT typically expires in 1 hour
            User: new UserInfo(
                Id: user.Id,
                Email: user.Email.Value,
                Username: user.Username.Value,
                CreatedAt: user.CreatedAt
            ),
            TokenType: "Bearer"
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Find refresh token
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidRefreshTokenException();
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
        if (user == null)
        {
            throw new InvalidRefreshTokenException();
        }

        // Revoke current refresh token
        refreshToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(refreshToken);

        // Generate new tokens
        var accessToken = _tokenGenerator.GenerateAccessToken(user.Id, user.Email.Value);
        var newRefreshToken = _tokenGenerator.GenerateRefreshToken();

        // Create new refresh token entity
        var newRefreshTokenEntity = new RefreshToken(
            Guid.NewGuid(), // Generate new Guid for refresh token
            newRefreshToken,
            user.Id,
            DateTime.UtcNow.AddDays(7) // Refresh tokens typically last 7 days
        );
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            User: new UserInfo(
                Id: user.Id,
                Email: user.Email.Value,
                Username: user.Username.Value,
                CreatedAt: user.CreatedAt
            ),
            TokenType: "Bearer"
        );
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (token != null && !token.IsRevoked)
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token);
        }
    }

    public Task<TokenValidationResponse> ValidateTokenAsync(string token)
    {
        var (isValid, userId, expiresAt) = _tokenGenerator.ValidateAndExtractClaims(token);

        return Task.FromResult(new TokenValidationResponse(
            IsValid: isValid,
            UserId: userId,
            ExpiresAt: expiresAt
        ));
    }
}