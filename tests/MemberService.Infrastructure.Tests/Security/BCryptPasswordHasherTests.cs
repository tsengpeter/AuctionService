using Xunit;
using FluentAssertions;
using MemberService.Infrastructure.Security;

namespace MemberService.Infrastructure.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_WithValidInput_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "MyPassword123!";
        var snowflakeId = 123456789L;

        // Act
        var hash = _hasher.HashPassword(password, snowflakeId);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2"); // bcrypt hash format
    }

    [Fact]
    public void HashPassword_WithDifferentSnowflakeIds_ProducesDifferentHashes()
    {
        // Arrange
        var password = "MyPassword123!";
        var snowflakeId1 = 123456789L;
        var snowflakeId2 = 987654321L;

        // Act
        var hash1 = _hasher.HashPassword(password, snowflakeId1);
        var hash2 = _hasher.HashPassword(password, snowflakeId2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_SameInputProducesDifferentHashes()
    {
        // Arrange
        var password = "MyPassword123!";
        var snowflakeId = 123456789L;

        // Act
        var hash1 = _hasher.HashPassword(password, snowflakeId);
        var hash2 = _hasher.HashPassword(password, snowflakeId);

        // Assert
        hash1.Should().NotBe(hash2); // bcrypt includes salt
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "MyPassword123!";
        var snowflakeId = 123456789L;
        var hash = _hasher.HashPassword(password, snowflakeId);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "MyPassword123!";
        var wrongPassword = "WrongPassword123!";
        var snowflakeId = 123456789L;
        var hash = _hasher.HashPassword(password, snowflakeId);

        // Act
        var result = _hasher.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        // Arrange
        var password = "MyPassword123!";
        var snowflakeId = 123456789L;
        var hash = _hasher.HashPassword(password, snowflakeId);

        // Act
        var result = _hasher.VerifyPassword("", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        var password = "MyPassword123!";
        var invalidHash = "invalid_hash";

        // Act
        var result = _hasher.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }
}
