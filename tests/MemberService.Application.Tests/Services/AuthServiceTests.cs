using FluentAssertions;
using MemberService.Application.Services;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemberService.Application.Tests.Services;

/// <summary>
/// Unit tests for AuthService.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ISnowflakeIdGenerator> _mockIdGenerator;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly Mock<IRefreshTokenGenerator> _mockRefreshTokenGenerator;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockIdGenerator = new Mock<ISnowflakeIdGenerator>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _mockRefreshTokenGenerator = new Mock<IRefreshTokenGenerator>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockRefreshTokenRepository.Object,
            _mockPasswordHasher.Object,
            _mockIdGenerator.Object,
            _mockJwtGenerator.Object,
            _mockRefreshTokenGenerator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidInput_ShouldCreateUserAndReturnAuthResponse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "ValidPassword123!";
        var username = "test user";
        var userId = 123456L;
        var passwordHash = "hashed_password";
        var accessToken = "jwt_token";
        var refreshToken = "refresh_token";

        _mockUserRepository.Setup(r => r.FindByEmailAsync(email, default))
            .ReturnsAsync((User?)null);

        _mockIdGenerator.Setup(g => g.GenerateId())
            .Returns(userId);

        _mockPasswordHasher.Setup(h => h.HashPassword(password, userId))
            .Returns(passwordHash);

        _mockJwtGenerator.Setup(g => g.GenerateToken(userId, email))
            .Returns(accessToken);

        _mockRefreshTokenGenerator.Setup(g => g.GenerateToken())
            .Returns(refreshToken);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), default))
            .Returns(Task.CompletedTask);

        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(email, password, username, default);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
        result.Username.Should().Be(username);
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);

        _mockUserRepository.Verify(r => r.FindByEmailAsync(email, default), Times.Once);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ShouldThrowException()
    {
        // Arrange
        var email = "existing@example.com";
        var password = "ValidPassword123!";
        var username = "test user";
        var existingUser = User.Create(
            123L,
            Email.Create(email),
            Username.Create(username),
            "hash");

        _mockUserRepository.Setup(r => r.FindByEmailAsync(email, default))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var result = async () => await _authService.RegisterAsync(email, password, username, default);
        await result.Should().ThrowAsync<EmailAlreadyExistsException>();

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "ValidPassword123!";
        var userId = 123456L;
        var username = "test user";
        var passwordHash = "hashed_password";
        var accessToken = "jwt_token";
        var refreshToken = "refresh_token";

        var user = User.Create(userId, Email.Create(email), Username.Create(username), passwordHash);

        _mockUserRepository.Setup(r => r.FindByEmailAsync(email, default))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(h => h.VerifyPassword(password, passwordHash))
            .Returns(true);

        _mockJwtGenerator.Setup(g => g.GenerateToken(userId, email))
            .Returns(accessToken);

        _mockRefreshTokenGenerator.Setup(g => g.GenerateToken())
            .Returns(refreshToken);

        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(email, password, default);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
        result.Username.Should().Be(username);
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);

        _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ShouldThrowException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "ValidPassword123!";

        _mockUserRepository.Setup(r => r.FindByEmailAsync(email, default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var result = async () => await _authService.LoginAsync(email, password, default);
        await result.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ShouldThrowException()
    {
        // Arrange
        var email = "test@example.com";
        var password = "WrongPassword";
        var userId = 123456L;
        var username = "test user";
        var passwordHash = "hashed_password";

        var user = User.Create(userId, Email.Create(email), Username.Create(username), passwordHash);

        _mockUserRepository.Setup(r => r.FindByEmailAsync(email, default))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(h => h.VerifyPassword(password, passwordHash))
            .Returns(false);

        // Act & Assert
        var result = async () => await _authService.LoginAsync(email, password, default);
        await result.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ShouldReturnNewAccessToken()
    {
        // Arrange
        var userId = 123456L;
        var email = "test@example.com";
        var tokenString = "valid_refresh_token";
        var newAccessToken = "new_jwt_token";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var refreshToken = RefreshToken.Create(tokenString, userId, expiresAt);
        var user = User.Create(userId, Email.Create(email), Username.Create("test user"), "hash");

        _mockRefreshTokenRepository.Setup(r => r.FindByTokenAsync(tokenString, default))
            .ReturnsAsync(refreshToken);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _mockJwtGenerator.Setup(g => g.GenerateToken(userId, email))
            .Returns(newAccessToken);

        // Act
        var result = await _authService.RefreshTokenAsync(tokenString, default);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(newAccessToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ShouldThrowException()
    {
        // Arrange
        var userId = 123456L;
        var tokenString = "expired_token";
        var expiresAt = DateTime.UtcNow.AddDays(-1); // Expired

        var refreshToken = RefreshToken.Create(tokenString, userId, expiresAt);

        _mockRefreshTokenRepository.Setup(r => r.FindByTokenAsync(tokenString, default))
            .ReturnsAsync(refreshToken);

        // Act & Assert
        var result = async () => await _authService.RefreshTokenAsync(tokenString, default);
        await result.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_ShouldRevokeToken()
    {
        // Arrange
        var userId = 123456L;
        var tokenString = "valid_refresh_token";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var refreshToken = RefreshToken.Create(tokenString, userId, expiresAt);

        _mockRefreshTokenRepository.Setup(r => r.FindByTokenAsync(tokenString, default))
            .ReturnsAsync(refreshToken);

        // Act
        await _authService.LogoutAsync(tokenString, default);

        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_TokenNotFound_ShouldNotThrow()
    {
        // Arrange
        var tokenString = "nonexistent_token";

        _mockRefreshTokenRepository.Setup(r => r.FindByTokenAsync(tokenString, default))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert - Should not throw (idempotent operation)
        await _authService.LogoutAsync(tokenString, default);
    }
}
