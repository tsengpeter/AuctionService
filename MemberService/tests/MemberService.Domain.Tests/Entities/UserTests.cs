using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace MemberService.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesUser()
    {
        // Arrange
        var id = 123456789L;
        var email = Email.Create("test@example.com").Value!;
        var phoneNumber = "+886912345678";
        var passwordHash = "hashedpassword";
        var username = Username.Create("Test User").Value!;

        // Act
        var user = new User(id, email, phoneNumber, passwordHash, username);

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.Username.Should().Be(username);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.RefreshTokens.Should().NotBeNull();
        user.RefreshTokens.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePassword_ChangesPasswordHashAndUpdatedAt()
    {
        // Arrange
        var user = CreateTestUser();
        var originalUpdatedAt = user.UpdatedAt;
        var newPasswordHash = "newhashedpassword";

        // Act
        user.UpdatePassword(newPasswordHash);

        // Assert
        user.PasswordHash.Should().Be(newPasswordHash);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateUsername_ChangesUsernameAndUpdatedAt()
    {
        // Arrange
        var user = CreateTestUser();
        var originalUpdatedAt = user.UpdatedAt;
        var newUsername = Username.Create("New Name").Value!;

        // Act
        user.UpdateUsername(newUsername);

        // Assert
        user.Username.Should().Be(newUsername);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    private static User CreateTestUser()
    {
        var id = 123456789L;
        var email = Email.Create("test@example.com").Value!;
        var phoneNumber = "+886912345678";
        var passwordHash = "hashedpassword";
        var username = Username.Create("Test User").Value!;
        return new User(id, email, phoneNumber, passwordHash, username);
    }
}