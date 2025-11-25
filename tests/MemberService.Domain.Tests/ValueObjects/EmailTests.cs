using Xunit;
using FluentAssertions;
using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ReturnsEmailValueObject()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var result = Email.Create(email);

        // Assert
        result.Value.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_ThrowsArgumentException(string email)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(email));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void Create_WithInvalidFormat_ThrowsArgumentException(string email)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(email));
    }

    [Fact]
    public void Create_WithEmailExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var email = new string('a', 250) + "@example.com"; // 261 characters

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(email));
    }

    [Fact]
    public void Create_WithUppercaseEmail_NormalizesToLowercase()
    {
        // Arrange
        var email = "TEST@EXAMPLE.COM";

        // Act
        var result = Email.Create(email);

        // Assert
        result.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void EqualityOperator_WithSameEmail_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        // Act & Assert
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentEmail_ReturnsFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        (email1 == email2).Should().BeFalse();
    }
}
