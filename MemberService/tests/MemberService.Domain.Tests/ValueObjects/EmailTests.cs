using MemberService.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace MemberService.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ReturnsSuccess()
    {
        // Arrange
        var validEmail = "test@example.com";

        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Fact]
    public void Create_WithNullEmail_ReturnsFailure()
    {
        // Act
        var result = Email.Create(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("電子郵件為必填欄位");
    }

    [Fact]
    public void Create_WithEmptyEmail_ReturnsFailure()
    {
        // Act
        var result = Email.Create("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("電子郵件為必填欄位");
    }

    [Fact]
    public void Create_WithWhitespaceEmail_ReturnsFailure()
    {
        // Act
        var result = Email.Create("   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("電子郵件為必填欄位");
    }

    [Fact]
    public void Create_WithTooLongEmail_ReturnsFailure()
    {
        // Arrange
        var longEmail = new string('a', 256) + "@example.com";

        // Act
        var result = Email.Create(longEmail);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("電子郵件長度不可超過 255 字元");
    }

    [Fact]
    public void Create_WithInvalidEmailFormat_ReturnsFailure()
    {
        // Arrange
        var invalidEmail = "invalid-email";

        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("請提供有效的電子郵件地址");
    }

    [Fact]
    public void Create_WithUppercaseEmail_ConvertsToLowercase()
    {
        // Arrange
        var uppercaseEmail = "TEST@EXAMPLE.COM";

        // Act
        var result = Email.Create(uppercaseEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Equals_WithSameEmail_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com").Value!;
        var email2 = Email.Create("test@example.com").Value!;

        // Act & Assert
        email1.Equals(email2).Should().BeTrue();
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentEmail_ReturnsFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com").Value!;
        var email2 = Email.Create("test2@example.com").Value!;

        // Act & Assert
        email1.Equals(email2).Should().BeFalse();
        (email1 != email2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsEmailValue()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }
}