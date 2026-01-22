using FluentValidation.TestHelper;
using MemberService.Application.DTOs.Auth;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator;

    public RegisterRequestValidatorTests()
    {
        _validator = new RegisterRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmailIsEmpty_ShouldFail(string? email)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = email!,
            Password = "ValidPassword123!",
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test.example.com")]
    public void Validate_InvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = email,
            Password = "ValidPassword123!",
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email must be a valid email address");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_PasswordIsEmpty_ShouldFail(string? password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = password!,
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is required");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Validate_PasswordTooShort_ShouldFail(string password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = password,
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must be at least 8 characters long");
    }

    [Fact]
    public void Validate_PasswordNoUppercase_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "lowercaseonly123!",
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one uppercase letter");
    }

    [Fact]
    public void Validate_PasswordNoLowercase_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "UPPERCASEONLY123!",
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one lowercase letter");
    }

    [Fact]
    public void Validate_PasswordNoDigit_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "PasswordOnly!",
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one digit");
    }

    [Fact]
    public void Validate_PasswordNoSpecialCharacter_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123",
            Username = "testuser",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one special character");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_UsernameIsEmpty_ShouldFail(string? username)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = username!,
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username is required");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Validate_UsernameTooShort_ShouldFail(string username)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = username,
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username must be at least 3 characters long");
    }

    [Fact]
    public void Validate_UsernameTooLong_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = new string('a', 51), // 51 characters
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username must not exceed 50 characters");
    }

    [Theory]
    [InlineData("user@name")]
    [InlineData("user.name")]
    [InlineData("user123")]
    [InlineData("user_name")]
    public void Validate_UsernameContainsInvalidCharacters_ShouldFail(string username)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = username,
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username can only contain letters and spaces");
    }

    [Fact]
    public void Validate_UsernameWithSpaces_ShouldPass()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "user name",
            PhoneNumber = "+886912345678"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

