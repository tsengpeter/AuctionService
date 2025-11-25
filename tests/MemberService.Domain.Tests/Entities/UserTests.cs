using Xunit;
using FluentAssertions;
using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsUserEntity()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("John Doe");
        var passwordHash = "bcrypt_hash_string";

        // Act
        var user = User.Create(123456789, email, username, passwordHash);

        // Assert
        user.Id.Should().Be(123456789);
        user.Email.Should().Be(email);
        user.Username.Should().Be(username);
        user.PasswordHash.Should().Be(passwordHash);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNegativeId_ThrowsArgumentException()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("John Doe");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            User.Create(-1, email, username, "hash"));
    }

    [Fact]
    public void Create_WithZeroId_ThrowsArgumentException()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("John Doe");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            User.Create(0, email, username, "hash"));
    }

    [Fact]
    public void Create_WithNullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            User.Create(123, null!, Username.Create("John Doe"), "hash"));
    }

    [Fact]
    public void Create_WithNullUsername_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            User.Create(123, Email.Create("test@example.com"), null!, "hash"));
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            User.Create(123, Email.Create("test@example.com"), Username.Create("John Doe"), ""));
    }

    [Fact]
    public void RefreshTokens_InitiallyEmpty()
    {
        // Arrange
        var user = User.Create(
            123456789,
            Email.Create("test@example.com"),
            Username.Create("John Doe"),
            "bcrypt_hash"
        );

        // Act & Assert
        user.RefreshTokens.Should().BeEmpty();
    }
}
