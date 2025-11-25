using Xunit;
using FluentAssertions;
using MemberService.Domain.Entities;

namespace MemberService.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsRefreshTokenEntity()
    {
        // Arrange
        var tokenValue = "token_string_value";
        var userId = 123456789L;
        var expiresAt = DateTime.UtcNow.AddDays(7);
        
        // Act
        var refreshToken = RefreshToken.Create(tokenValue, userId, expiresAt);
        
        // Assert
        refreshToken.Token.Should().Be(tokenValue);
        refreshToken.UserId.Should().Be(userId);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void Create_WithEmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            RefreshToken.Create("", 123, DateTime.UtcNow.AddDays(7)));
    }
    
    [Fact]
    public void Create_WithZeroUserId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            RefreshToken.Create("token", 0, DateTime.UtcNow.AddDays(7)));
    }
    
    [Fact]
    public void Create_WithNegativeUserId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            RefreshToken.Create("token", -1, DateTime.UtcNow.AddDays(7)));
    }
    
    [Fact]
    public void IsExpired_WhenTokenExpired_ReturnsTrue()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(
            "token",
            123,
            DateTime.UtcNow.AddSeconds(-1) // Already expired
        );
        
        // Act & Assert
        refreshToken.IsExpired.Should().BeTrue();
    }
    
    [Fact]
    public void IsExpired_WhenTokenNotExpired_ReturnsFalse()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(
            "token",
            123,
            DateTime.UtcNow.AddDays(1) // Expires in 1 day
        );
        
        // Act & Assert
        refreshToken.IsExpired.Should().BeFalse();
    }
    
    [Fact]
    public void IsValid_WhenTokenNotExpiredAndNotRevoked_ReturnsTrue()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(
            "token",
            123,
            DateTime.UtcNow.AddDays(1)
        );
        
        // Act & Assert
        refreshToken.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void IsValid_WhenTokenExpired_ReturnsFalse()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(
            "token",
            123,
            DateTime.UtcNow.AddSeconds(-1)
        );
        
        // Act & Assert
        refreshToken.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void IsValid_WhenTokenRevoked_ReturnsFalse()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(
            "token",
            123,
            DateTime.UtcNow.AddDays(1)
        );
        refreshToken.Revoke();
        
        // Act & Assert
        refreshToken.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void Revoke_SetsIsRevokedToTrue()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(
            "token",
            123,
            DateTime.UtcNow.AddDays(1)
        );
        
        // Act
        refreshToken.Revoke();
        
        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
    }
    
    [Fact]
    public void Revoke_MultipleInvocations_RemainsRevoked()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(
            "token",
            123,
            DateTime.UtcNow.AddDays(1)
        );
        
        // Act
        refreshToken.Revoke();
        refreshToken.Revoke();
        
        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
    }
}
