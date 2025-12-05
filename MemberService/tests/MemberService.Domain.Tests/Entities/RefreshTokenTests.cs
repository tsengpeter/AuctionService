using MemberService.Domain.Entities;
using Xunit;
using FluentAssertions;

namespace MemberService.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesRefreshToken()
    {
        // Arrange
        var id = Guid.NewGuid();
        var token = "base64encodedtoken";
        var userId = 123456789L;
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var refreshToken = new RefreshToken(id, token, userId, expiresAt);

        // Assert
        refreshToken.Id.Should().Be(id);
        refreshToken.Token.Should().Be(token);
        refreshToken.UserId.Should().Be(userId);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Revoke_SetsIsRevokedToTrue()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();

        // Act
        refreshToken.Revoke();

        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
        refreshToken.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ReturnsTrue()
    {
        // Arrange
        var expiredTime = DateTime.UtcNow.AddDays(-1);
        var refreshToken = new RefreshToken(Guid.NewGuid(), "token", 1L, expiredTime);

        // Act & Assert
        refreshToken.IsExpired.Should().BeTrue();
        refreshToken.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();

        // Act & Assert
        refreshToken.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenRevoked_ReturnsFalse()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        refreshToken.Revoke();

        // Act & Assert
        refreshToken.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var expiredTime = DateTime.UtcNow.AddDays(-1);
        var refreshToken = new RefreshToken(Guid.NewGuid(), "token", 1L, expiredTime);

        // Act & Assert
        refreshToken.IsValid.Should().BeFalse();
    }

    private static RefreshToken CreateTestRefreshToken()
    {
        var id = Guid.NewGuid();
        var token = "base64encodedtoken";
        var userId = 123456789L;
        var expiresAt = DateTime.UtcNow.AddDays(7);
        return new RefreshToken(id, token, userId, expiresAt);
    }
}