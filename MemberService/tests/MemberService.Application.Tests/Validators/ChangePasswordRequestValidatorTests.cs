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
            OldPassword = "OldPassword123",
            NewPassword = "NewSecurePassword456"
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
            NewPassword = "NewSecurePassword456"
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
            OldPassword = "OldPassword123",
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
            OldPassword = "OldPassword123",
            NewPassword = "Short"
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
            OldPassword = "OldPassword123",
            NewPassword = new string('a', 129)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must not exceed 128 characters.");
    }
}