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
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IIdGenerator idGenerator,
        IVerificationCodeService verificationCodeService,
        IEmailService emailService,
        ISmsService smsService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _idGenerator = idGenerator;
        _verificationCodeService = verificationCodeService;
        _emailService = emailService;
        _smsService = smsService;
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

        // Check if phone number already exists
        if (await _userRepository.ExistsByPhoneNumberAsync(request.PhoneNumber))
        {
            throw new PhoneAlreadyExistsException(request.PhoneNumber);
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

        var user = new User(userId, email, request.PhoneNumber, passwordHash, username);
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
        var (isValid, userId, expiresAt, errorMessage) = _tokenGenerator.ValidateAndExtractClaims(token);

        return Task.FromResult(new TokenValidationResponse(
            IsValid: isValid,
            UserId: userId,
            ExpiresAt: expiresAt,
            ErrorMessage: errorMessage
        ));
    }

    /// <summary>
    /// Request email verification code
    /// </summary>
    public async Task<VerificationResponse> RequestEmailVerificationAsync(long userId, CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId.ToString());
        }

        if (user.EmailVerified)
        {
            throw new InvalidOperationException("Email is already verified");
        }

        try
        {
            var code = await _verificationCodeService.GenerateAndStoreAsync(
                userId,
                "EmailVerification",
                "Email",
                user.Email.Value,
                cancellationToken
            );

            // Send email with verification code
            await _emailService.SendVerificationCodeAsync(user.Email.Value, code, cancellationToken);

            return new VerificationResponse(true, $"Verification code sent to {user.Email.Value}. Valid for 5 minutes.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("wait"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"wait (\d+) seconds");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seconds))
            {
                throw new VerificationCodeCooldownException(seconds);
            }
            throw;
        }
    }

    /// <summary>
    /// Request phone verification code
    /// </summary>
    public async Task<VerificationResponse> RequestPhoneVerificationAsync(long userId, CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId.ToString());
        }

        if (user.PhoneNumberVerified)
        {
            throw new InvalidOperationException("Phone number is already verified");
        }

        try
        {
            var code = await _verificationCodeService.GenerateAndStoreAsync(
                userId,
                "PhoneVerification",
                "Sms",
                user.PhoneNumber,
                cancellationToken
            );

            // Send SMS with verification code
            await _smsService.SendVerificationCodeAsync(user.PhoneNumber, code, cancellationToken);

            return new VerificationResponse(true, $"Verification code sent to {user.PhoneNumber}. Valid for 5 minutes.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("wait"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"wait (\d+) seconds");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seconds))
            {
                throw new VerificationCodeCooldownException(seconds);
            }
            throw;
        }
    }

    /// <summary>
    /// Verify email verification code
    /// </summary>
    public async Task<VerificationResponse> VerifyEmailAsync(long userId, string code, CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId.ToString());
        }

        // Get verification code from Redis
        var verificationCode = await _verificationCodeService.GetAsync(
            userId,
            "EmailVerification",
            "Email",
            cancellationToken
        );

        if (verificationCode == null)
        {
            throw new InvalidVerificationCodeException("Verification code not found");
        }

        // Check if expired
        if (verificationCode.IsExpired)
        {
            await _verificationCodeService.DeleteAsync(userId, "EmailVerification", "Email", cancellationToken);
            throw new InvalidVerificationCodeException("Verification code has expired");
        }

        // Check if max attempts exceeded
        if (verificationCode.IsMaxAttemptsExceeded)
        {
            await _verificationCodeService.DeleteAsync(userId, "EmailVerification", "Email", cancellationToken);
            throw new InvalidVerificationCodeException("Too many failed attempts");
        }

        // Verify code
        if (verificationCode.Code != code)
        {
            var updated = await _verificationCodeService.IncrementAttemptAsync(
                userId,
                "EmailVerification",
                "Email",
                cancellationToken
            );

            if (updated?.IsMaxAttemptsExceeded ?? false)
            {
                throw new InvalidVerificationCodeException("Too many failed attempts");
            }

            var remainingAttempts = 3 - (updated?.AttemptCount ?? 0);
            throw new InvalidVerificationCodeException($"Invalid code. {remainingAttempts} attempts remaining.");
        }

        // Code is correct - update user verification status
        user.VerifyEmail();
        await _userRepository.UpdateAsync(user);

        // Delete verification code from Redis
        await _verificationCodeService.DeleteAsync(userId, "EmailVerification", "Email", cancellationToken);

        return new VerificationResponse(true, "Email verified successfully");
    }

    /// <summary>
    /// Verify phone verification code
    /// </summary>
    public async Task<VerificationResponse> VerifyPhoneAsync(long userId, string code, CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId.ToString());
        }

        // Get verification code from Redis
        var verificationCode = await _verificationCodeService.GetAsync(
            userId,
            "PhoneVerification",
            "Sms",
            cancellationToken
        );

        if (verificationCode == null)
        {
            throw new InvalidVerificationCodeException("Verification code not found");
        }

        // Check if expired
        if (verificationCode.IsExpired)
        {
            await _verificationCodeService.DeleteAsync(userId, "PhoneVerification", "Sms", cancellationToken);
            throw new InvalidVerificationCodeException("Verification code has expired");
        }

        // Check if max attempts exceeded
        if (verificationCode.IsMaxAttemptsExceeded)
        {
            await _verificationCodeService.DeleteAsync(userId, "PhoneVerification", "Sms", cancellationToken);
            throw new InvalidVerificationCodeException("Too many failed attempts");
        }

        // Verify code
        if (verificationCode.Code != code)
        {
            var updated = await _verificationCodeService.IncrementAttemptAsync(
                userId,
                "PhoneVerification",
                "Sms",
                cancellationToken
            );

            if (updated?.IsMaxAttemptsExceeded ?? false)
            {
                throw new InvalidVerificationCodeException("Too many failed attempts");
            }

            var remainingAttempts = 3 - (updated?.AttemptCount ?? 0);
            throw new InvalidVerificationCodeException($"Invalid code. {remainingAttempts} attempts remaining.");
        }

        // Code is correct - update user verification status
        user.VerifyPhoneNumber();
        await _userRepository.UpdateAsync(user);

        // Delete verification code from Redis
        await _verificationCodeService.DeleteAsync(userId, "PhoneVerification", "Sms", cancellationToken);

        return new VerificationResponse(true, "Phone number verified successfully");
    }
}