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
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IIdGenerator> _idGeneratorMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _idGeneratorMock = new Mock<IIdGenerator>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
            _idGeneratorMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ShouldCreateUserAndReturnAuthResponse()
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
        var accessToken = "access_token";
        var refreshToken = "refresh_token";

        _idGeneratorMock.Setup(x => x.GenerateId()).Returns(userId);
        _passwordHasherMock.Setup(x => x.HashPassword(request.Password, userId)).Returns(passwordHash);
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<Email>())).ReturnsAsync(false);
        _tokenGeneratorMock.Setup(x => x.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>()))
            .Returns(accessToken);
        _tokenGeneratorMock.Setup(x => x.GenerateRefreshToken()).Returns(refreshToken);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.TokenType.Should().Be("Bearer");

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
}