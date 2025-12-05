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
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userService = new UserService(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ShouldReturnUserProfile()
    {
        // Arrange
        var userId = 1L;
        var user = new User(userId, Email.Create("test@example.com").Value!, "hash", Username.Create("testuser").Value!);

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
        var user = new User(userId, Email.Create("test@example.com").Value!, "hash", Username.Create("testuser").Value!);

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
}