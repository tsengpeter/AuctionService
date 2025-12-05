using FluentValidation.TestHelper;
using MemberService.Application.DTOs.Auth;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!"
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
        var request = new LoginRequest
        {
            Email = email!,
            Password = "ValidPassword123!"
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
        var request = new LoginRequest
        {
            Email = email,
            Password = "ValidPassword123!"
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
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = password!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is required");
    }
}