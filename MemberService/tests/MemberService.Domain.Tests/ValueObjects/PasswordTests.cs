using MemberService.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace MemberService.Domain.Tests.ValueObjects;

public class PasswordTests
{
    [Fact]
    public void Create_WithValidPassword_ReturnsSuccess()
    {
        // Arrange
        var validPassword = "SecurePassword123!";

        // Act
        var result = Password.Create(validPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(validPassword);
    }

    [Fact]
    public void Create_WithNullPassword_ReturnsFailure()
    {
        // Act
        var result = Password.Create(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼為必填欄位");
    }

    [Fact]
    public void Create_WithEmptyPassword_ReturnsFailure()
    {
        // Act
        var result = Password.Create("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼為必填欄位");
    }

    [Fact]
    public void Create_WithWhitespacePassword_ReturnsFailure()
    {
        // Act
        var result = Password.Create("   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼為必填欄位");
    }

    [Fact]
    public void Create_WithTooShortPassword_ReturnsFailure()
    {
        // Arrange
        var shortPassword = "Short1!";

        // Act
        var result = Password.Create(shortPassword);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼長度至少 8 個字元");
    }

    [Fact]
    public void Create_WithTooLongPassword_ReturnsFailure()
    {
        // Arrange
        var longPassword = new string('a', 129);

        // Act
        var result = Password.Create(longPassword);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼長度不可超過 128 字元");
    }

    [Fact]
    public void Create_WithNoUpperCase_ReturnsFailure()
    {
        // Arrange
        var noUpper = "password123!";

        // Act
        var result = Password.Create(noUpper);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼必須包含至少一個大寫字母");
    }

    [Fact]
    public void Create_WithNoLowerCase_ReturnsFailure()
    {
        // Arrange
        var noLower = "PASSWORD123!";

        // Act
        var result = Password.Create(noLower);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼必須包含至少一個小寫字母");
    }

    [Fact]
    public void Create_WithNoDigit_ReturnsFailure()
    {
        // Arrange
        var noDigit = "Password!";

        // Act
        var result = Password.Create(noDigit);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼必須包含至少一個數字");
    }

    [Fact]
    public void Create_WithNoSpecialChar_ReturnsFailure()
    {
        // Arrange
        var noSpecial = "Password123";

        // Act
        var result = Password.Create(noSpecial);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("密碼必須包含至少一個特殊符號");
    }

    [Fact]
    public void Create_WithMinimumLengthPassword_ReturnsSuccess()
    {
        // Arrange
        var minPassword = "Pass123!";

        // Act
        var result = Password.Create(minPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(minPassword);
    }

    [Fact]
    public void Create_WithMaximumLengthPassword_ReturnsSuccess()
    {
        // Arrange
        // Create a long password that satisfies all conditions
        var maxPassword = new string('a', 125) + "A1!";

        // Act
        var result = Password.Create(maxPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(maxPassword);
    }

    [Fact]
    public void Equals_WithSamePassword_ReturnsTrue()
    {
        // Arrange
        var password1 = Password.Create("Password123!").Value!;
        var password2 = Password.Create("Password123!").Value!;

        // Act & Assert
        password1.Equals(password2).Should().BeTrue();
        (password1 == password2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentPassword_ReturnsFalse()
    {
        // Arrange
        var password1 = Password.Create("Password123!").Value!;
        var password2 = Password.Create("Different123!").Value!;

        // Act & Assert
        password1.Equals(password2).Should().BeFalse();
        (password1 != password2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsMaskedPassword()
    {
        // Arrange
        var password = Password.Create("MyPassword1!").Value!;

        // Act
        var result = password.ToString();

        // Assert
        result.Should().Be("************");
    }
}