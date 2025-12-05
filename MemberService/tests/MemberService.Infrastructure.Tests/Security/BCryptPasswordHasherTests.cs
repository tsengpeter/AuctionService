using MemberService.Infrastructure.Security;
using Xunit;
using FluentAssertions;

namespace MemberService.Infrastructure.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher;

    public BCryptPasswordHasherTests()
    {
        _hasher = new BCryptPasswordHasher();
    }

    [Fact]
    public void HashPassword_ReturnsNonEmptyString()
    {
        // Arrange
        var password = "testpassword";
        var userId = 123L;

        // Act
        var hash = _hasher.HashPassword(password, userId);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_WithSamePasswordAndUserId_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "testpassword";
        var userId = 123L;

        // Act
        var hash1 = _hasher.HashPassword(password, userId);
        var hash2 = _hasher.HashPassword(password, userId);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt generates different salts
    }

    [Fact]
    public void HashPassword_WithDifferentUserIds_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "testpassword";
        var userId1 = 123L;
        var userId2 = 456L;

        // Act
        var hash1 = _hasher.HashPassword(password, userId1);
        var hash2 = _hasher.HashPassword(password, userId2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPasswordAndUserId_ReturnsTrue()
    {
        // Arrange
        var password = "testpassword";
        var userId = 123L;
        var hash = _hasher.HashPassword(password, userId);

        // Act
        var result = _hasher.VerifyPassword(password, userId, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "testpassword";
        var wrongPassword = "wrongpassword";
        var userId = 123L;
        var hash = _hasher.HashPassword(correctPassword, userId);

        // Act
        var result = _hasher.VerifyPassword(wrongPassword, userId, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithWrongUserId_ReturnsFalse()
    {
        // Arrange
        var password = "testpassword";
        var correctUserId = 123L;
        var wrongUserId = 456L;
        var hash = _hasher.HashPassword(password, correctUserId);

        // Act
        var result = _hasher.VerifyPassword(password, wrongUserId, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        var password = "testpassword";
        var userId = 123L;
        var invalidHash = "invalidhash";

        // Act
        var result = _hasher.VerifyPassword(password, userId, invalidHash);

        // Assert
        result.Should().BeFalse();
    }
}