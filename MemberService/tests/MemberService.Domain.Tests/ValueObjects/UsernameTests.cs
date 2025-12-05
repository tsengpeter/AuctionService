using MemberService.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace MemberService.Domain.Tests.ValueObjects;

public class UsernameTests
{
    [Fact]
    public void Create_WithValidUsername_ReturnsSuccess()
    {
        // Arrange
        var validUsername = "John Doe";

        // Act
        var result = Username.Create(validUsername);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(validUsername);
    }

    [Fact]
    public void Create_WithNullUsername_ReturnsFailure()
    {
        // Act
        var result = Username.Create(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("使用者名稱為必填欄位");
    }

    [Fact]
    public void Create_WithEmptyUsername_ReturnsFailure()
    {
        // Act
        var result = Username.Create("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("使用者名稱為必填欄位");
    }

    [Fact]
    public void Create_WithWhitespaceUsername_ReturnsFailure()
    {
        // Act
        var result = Username.Create("   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("使用者名稱為必填欄位");
    }

    [Fact]
    public void Create_WithTooShortUsername_ReturnsFailure()
    {
        // Arrange
        var shortUsername = "Jo";

        // Act
        var result = Username.Create(shortUsername);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("使用者名稱長度至少 3 個字元");
    }

    [Fact]
    public void Create_WithTooLongUsername_ReturnsFailure()
    {
        // Arrange
        var longUsername = new string('a', 51);

        // Act
        var result = Username.Create(longUsername);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("使用者名稱長度不可超過 50 字元");
    }

    [Fact]
    public void Create_WithInvalidCharacters_ReturnsFailure()
    {
        // Arrange
        var invalidUsername = "John@Doe";

        // Act
        var result = Username.Create(invalidUsername);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Username can only contain letters, numbers, underscores, and spaces");
    }

    [Fact]
    public void Create_WithNumbersAndUnderscores_ReturnsSuccess()
    {
        // Arrange
        var validUsername = "John123_Doe";

        // Act
        var result = Username.Create(validUsername);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(validUsername);
    }

    [Fact]
    public void Create_WithMinimumLengthUsername_ReturnsSuccess()
    {
        // Arrange
        var minUsername = "Joe";

        // Act
        var result = Username.Create(minUsername);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(minUsername);
    }

    [Fact]
    public void Create_WithMaximumLengthUsername_ReturnsSuccess()
    {
        // Arrange
        var maxUsername = new string('a', 50);

        // Act
        var result = Username.Create(maxUsername);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(maxUsername);
    }

    [Fact]
    public void Create_WithLeadingTrailingSpaces_TrimsSpaces()
    {
        // Arrange
        var usernameWithSpaces = "  John Doe  ";

        // Act
        var result = Username.Create(usernameWithSpaces);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("John Doe");
    }

    [Fact]
    public void Equals_WithSameUsername_ReturnsTrue()
    {
        // Arrange
        var username1 = Username.Create("John Doe").Value!;
        var username2 = Username.Create("John Doe").Value!;

        // Act & Assert
        username1.Equals(username2).Should().BeTrue();
        (username1 == username2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentUsername_ReturnsFalse()
    {
        // Arrange
        var username1 = Username.Create("John Doe").Value!;
        var username2 = Username.Create("Jane Doe").Value!;

        // Act & Assert
        username1.Equals(username2).Should().BeFalse();
        (username1 != username2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsUsernameValue()
    {
        // Arrange
        var username = Username.Create("John Doe").Value!;

        // Act
        var result = username.ToString();

        // Assert
        result.Should().Be("John Doe");
    }
}