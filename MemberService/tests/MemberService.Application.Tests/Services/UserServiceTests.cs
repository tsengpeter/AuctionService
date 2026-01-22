using FluentAssertions;
using MemberService.Application.DTOs.Users;
using MemberService.Application.Services;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace MemberService.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _userService = new UserService(_userRepositoryMock.Object, _refreshTokenRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ShouldReturnUserProfile()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "hash", Username.Create("testuser").Value!);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetCurrentUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.Username.Should().Be("testuser");
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.UpdatedAt.Should().Be(user.UpdatedAt);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = 1L;
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var act = () => _userService.GetCurrentUserAsync(userId);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ShouldReturnPublicProfile()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "hash", Username.Create("testuser").Value!);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Username.Should().Be("testuser");
        result.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = 1L;
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var act = () => _userService.GetUserByIdAsync(userId);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_ExistingUser_UpdatesUsername_ShouldReturnUpdatedProfile()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "hash", Username.Create("testuser").Value!);
        var request = new UpdateProfileRequest { Username = "newusername" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

        // Act
        var result = await _userService.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Username.Should().Be("newusername");
        result.Email.Should().Be("test@example.com");
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_ExistingUser_UpdatesEmail_ShouldReturnUpdatedProfile()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "hash", Username.Create("testuser").Value!);
        var request = new UpdateProfileRequest { Email = "newemail@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<Email>())).ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

        // Act
        var result = await _userService.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("newemail@example.com");
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_ExistingUser_UpdatesBoth_ShouldReturnUpdatedProfile()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "hash", Username.Create("testuser").Value!);
        var request = new UpdateProfileRequest { Username = "newusername", Email = "newemail@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<Email>())).ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

        // Act
        var result = await _userService.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Username.Should().Be("newusername");
        result.Email.Should().Be("newemail@example.com");
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = 1L;
        var request = new UpdateProfileRequest { Username = "newusername" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var act = () => _userService.UpdateProfileAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_EmailAlreadyExists_ShouldThrowEmailAlreadyExistsException()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "hash", Username.Create("testuser").Value!);
        var request = new UpdateProfileRequest { Email = "existing@example.com" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<Email>())).ReturnsAsync(true);

        // Act
        var act = () => _userService.UpdateProfileAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidOldPassword_ShouldUpdatePasswordAndRevokeTokens()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "oldhash", Username.Create("testuser").Value!);
        var request = new ChangePasswordRequest { OldPassword = "oldpass", NewPassword = "newpass" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("oldpass", userId, "oldhash")).Returns(true);
        _passwordHasherMock.Setup(x => x.HashPassword("newpass", userId)).Returns("newhash");
        _userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);
        _refreshTokenRepositoryMock.Setup(x => x.RevokeAllUserTokensAsync(userId)).Returns(Task.CompletedTask);

        // Act
        await _userService.ChangePasswordAsync(userId, request);

        // Assert
        _passwordHasherMock.Verify(x => x.VerifyPassword("oldpass", userId, "oldhash"), Times.Once);
        _passwordHasherMock.Verify(x => x.HashPassword("newpass", userId), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _refreshTokenRepositoryMock.Verify(x => x.RevokeAllUserTokensAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_InvalidOldPassword_ShouldThrowInvalidPasswordException()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "+886912345678", "oldhash", Username.Create("testuser").Value!);
        var request = new ChangePasswordRequest { OldPassword = "wrongpass", NewPassword = "newpass" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("wrongpass", userId, "oldhash")).Returns(false);

        // Act
        var act = () => _userService.ChangePasswordAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPasswordException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = 1L;
        var request = new ChangePasswordRequest { OldPassword = "oldpass", NewPassword = "newpass" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var act = () => _userService.ChangePasswordAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
