using FluentAssertions;
using MemberService.Application.DTOs.Auth;
using MemberService.Application.Services;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace MemberService.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IIdGenerator> _idGeneratorMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _idGeneratorMock = new Mock<IIdGenerator>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
            _idGeneratorMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ShouldCreateUserAndReturnRegisterResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "testuser"
        };

        var userId = 1L;
        var passwordHash = "hashed_password";

        _idGeneratorMock.Setup(x => x.GenerateId()).Returns(userId);
        _passwordHasherMock.Setup(x => x.HashPassword(request.Password, userId)).Returns(passwordHash);
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<Email>())).ReturnsAsync(false);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(request.Email);
        result.User.Username.Should().Be(request.Username);
        result.Message.Should().NotBeNullOrEmpty();

        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u =>
            u.Id == userId &&
            u.Email.Value == request.Email &&
            u.Username.Value == request.Username)), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ShouldThrowEmailAlreadyExistsException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "ValidPassword123!",
            Username = "testuser"
        };

        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<Email>())).ReturnsAsync(true);

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!"
        };

        var user = new User(1L, Email.Create(request.Email).Value!, "hashed_password", Username.Create("testuser").Value!);
        var accessToken = "access_token";
        var refreshToken = "refresh_token";

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<Email>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.Id, user.PasswordHash)).Returns(true);
        _tokenGeneratorMock.Setup(x => x.GenerateAccessToken(user.Id, user.Email.Value))
            .Returns(accessToken);
        _tokenGeneratorMock.Setup(x => x.GenerateRefreshToken()).Returns(refreshToken);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "ValidPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<Email>())).ReturnsAsync((User?)null);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        var user = new User(1L, Email.Create(request.Email).Value!, "hashed_password", Username.Create("testuser").Value!);

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<Email>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.Id, user.PasswordHash)).Returns(false);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ShouldReturnNewAuthResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest("valid_refresh_token");
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "hash", Username.Create("testuser").Value!);
        var refreshTokenEntity = new RefreshToken(Guid.NewGuid(), "valid_refresh_token", userId, DateTime.UtcNow.AddDays(1));
        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(request.RefreshToken))
            .ReturnsAsync(refreshTokenEntity);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(x => x.GenerateAccessToken(userId, user.Email.Value))
            .Returns(newAccessToken);
        _tokenGeneratorMock.Setup(x => x.GenerateRefreshToken()).Returns(newRefreshToken);

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRefreshToken);

        _refreshTokenRepositoryMock.Verify(x => x.UpdateAsync(It.Is<RefreshToken>(rt => rt.IsRevoked)), Times.Once);
        _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.Is<RefreshToken>(rt =>
            rt.UserId == userId && rt.Token == newRefreshToken)), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ShouldThrowInvalidRefreshTokenException()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid_token");

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(request.RefreshToken))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = () => _authService.RefreshTokenAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ShouldThrowInvalidRefreshTokenException()
    {
        // Arrange
        var request = new RefreshTokenRequest("expired_token");
        var refreshTokenEntity = new RefreshToken(Guid.NewGuid(), "expired_token", 1L, DateTime.UtcNow.AddDays(-1));

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(request.RefreshToken))
            .ReturnsAsync(refreshTokenEntity);

        // Act
        var act = () => _authService.RefreshTokenAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_ShouldRevokeToken()
    {
        // Arrange
        var refreshToken = "valid_token";
        var refreshTokenEntity = new RefreshToken(Guid.NewGuid(), refreshToken, 1L, DateTime.UtcNow.AddDays(1));

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(refreshToken))
            .ReturnsAsync(refreshTokenEntity);

        // Act
        await _authService.LogoutAsync(refreshToken);

        // Assert
        _refreshTokenRepositoryMock.Verify(x => x.UpdateAsync(It.Is<RefreshToken>(rt => rt.IsRevoked)), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_InvalidToken_ShouldNotThrow()
    {
        // Arrange
        var refreshToken = "invalid_token";

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(refreshToken))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = () => _authService.LogoutAsync(refreshToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ShouldReturnValidResponse()
    {
        // Arrange
        var token = "valid_jwt_token";
        var expectedUserId = 1234567890123456L;
        var expectedExpiresAt = DateTime.UtcNow.AddMinutes(15);

        _tokenGeneratorMock.Setup(x => x.ValidateAndExtractClaims(token))
            .Returns((true, expectedUserId, expectedExpiresAt, null));

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(expectedUserId);
        result.ExpiresAt.Should().Be(expectedExpiresAt);
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ShouldReturnInvalidResponse()
    {
        // Arrange
        var token = "invalid_jwt_token";

        _tokenGeneratorMock.Setup(x => x.ValidateAndExtractClaims(token))
            .Returns((false, null, null, "Token signature is invalid"));

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.ErrorMessage.Should().Be("Token signature is invalid");
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ShouldReturnInvalidResponse()
    {
        // Arrange
        var token = "expired_jwt_token";

        _tokenGeneratorMock.Setup(x => x.ValidateAndExtractClaims(token))
            .Returns((false, null, null, "Token has expired"));

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.ErrorMessage.Should().Be("Token has expired");
    }
}