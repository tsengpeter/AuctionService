using FluentValidation.TestHelper;
using MemberService.Application.DTOs.Users;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

/// <summary>
/// Tests for ChangePasswordRequestValidator
/// Validates old password is provided and new password meets requirements
/// </summary>
public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidPasswords_ShouldPass()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "CurrentPassword123",
            NewPassword = "NewPassword456"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOldPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = string.Empty,
            NewPassword = "NewPassword456"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OldPassword);
    }

    [Fact]
    public void Validate_WithNullOldPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = null!,
            NewPassword = "NewPassword456"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OldPassword);
    }

    [Fact]
    public void Validate_WithWhitespaceOldPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "   ",
            NewPassword = "NewPassword456"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OldPassword);
    }

    [Fact]
    public void Validate_WithEmptyNewPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "CurrentPassword123",
            NewPassword = string.Empty
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithNullNewPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "CurrentPassword123",
            NewPassword = null!
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithNewPasswordTooShort_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "CurrentPassword123",
            NewPassword = "Pass"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithNewPasswordTooLong_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "CurrentPassword123",
            NewPassword = new string('a', 129)
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithNewPasswordAt8Characters_ShouldPass()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "CurrentPassword123",
            NewPassword = "12345678"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNewPasswordAt128Characters_ShouldPass()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "CurrentPassword123",
            NewPassword = new string('a', 128)
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
