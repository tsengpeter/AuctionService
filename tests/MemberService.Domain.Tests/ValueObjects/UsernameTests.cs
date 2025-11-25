using Xunit;
using FluentAssertions;
using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Tests.ValueObjects;

public class UsernameTests
{
    [Fact]
    public void Create_WithValidUsername_ReturnsUsernameValueObject()
    {
        // Arrange
        var username = "John Doe";

        // Act
        var result = Username.Create(username);

        // Assert
        result.Value.Should().Be("John Doe");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyUsername_ThrowsArgumentException(string username)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(username));
    }

    [Fact]
    public void Create_WithUsernameTooShort_ThrowsArgumentException()
    {
        // Arrange
        var username = "Jo"; // 2 characters, minimum is 3

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(username));
    }

    [Fact]
    public void Create_WithUsernameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var username = new string('a', 51); // 51 characters, maximum is 50

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(username));
    }

    [Theory]
    [InlineData("John123")] // Contains numbers
    [InlineData("John-Doe")] // Contains hyphen
    [InlineData("John_Doe")] // Contains underscore
    [InlineData("John@Doe")] // Contains special character
    public void Create_WithInvalidCharacters_ThrowsArgumentException(string username)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(username));
    }

    [Theory]
    [InlineData("John")]
    [InlineData("Mary Jane")]
    [InlineData("José García")] // Unicode letters
    [InlineData("李明")] // CJK characters
    public void Create_WithValidLettersAndSpaces_ReturnsUsernameValueObject(string username)
    {
        // Act
        var result = Username.Create(username);

        // Assert
        result.Value.Should().Be(username);
    }

    [Fact]
    public void Create_WithExactlyMinimumLength_ReturnsUsernameValueObject()
    {
        // Arrange
        var username = "ABC"; // 3 characters

        // Act
        var result = Username.Create(username);

        // Assert
        result.Value.Should().Be("ABC");
    }

    [Fact]
    public void Create_WithExactlyMaximumLength_ReturnsUsernameValueObject()
    {
        // Arrange
        var username = new string('A', 50); // 50 characters

        // Act
        var result = Username.Create(username);

        // Assert
        result.Value.Should().HaveLength(50);
    }

    [Fact]
    public void EqualityOperator_WithSameUsername_ReturnsTrue()
    {
        // Arrange
        var username1 = Username.Create("John Doe");
        var username2 = Username.Create("John Doe");

        // Act & Assert
        (username1 == username2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentUsername_ReturnsFalse()
    {
        // Arrange
        var username1 = Username.Create("John Doe");
        var username2 = Username.Create("Jane Doe");

        // Act & Assert
        (username1 == username2).Should().BeFalse();
    }
}
