using FluentAssertions;
using FluentValidation.TestHelper;
using MemberService.Application.DTOs.Users;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewSecurePassword456!"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Fail_When_OldPassword_Is_Null_Or_Empty_Or_Whitespace(string? oldPassword)
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = "NewSecurePassword456!"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OldPassword)
            .WithErrorMessage("Old password is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Fail_When_NewPassword_Is_Null_Or_Empty_Or_Whitespace(string? newPassword)
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = newPassword
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password is required.");
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Is_Too_Short()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "Short1!"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must be at least 8 characters long.");
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Is_Too_Long()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = new string('a', 129)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must not exceed 128 characters.");
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Missing_UpperCase()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "password123!"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must contain at least one uppercase letter.");
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Missing_LowerCase()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "PASSWORD123!"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must contain at least one lowercase letter.");
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Missing_Digit()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "Password!!!!"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must contain at least one digit.");
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Missing_SpecialChar()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "Password1234"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must contain at least one special character.");
    }
}