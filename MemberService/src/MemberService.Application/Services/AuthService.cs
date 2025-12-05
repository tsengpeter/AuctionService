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
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIdGenerator _idGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IIdGenerator idGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _idGenerator = idGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
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

        // Generate tokens
        var accessToken = _tokenGenerator.GenerateAccessToken(user.Id, user.Email.Value);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1), // JWT typically expires in 1 hour
            TokenType: "Bearer"
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

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1), // JWT typically expires in 1 hour
            TokenType: "Bearer"
        );
    }
}