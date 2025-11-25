using FluentValidation.TestHelper;
using MemberService.Application.DTOs.Users;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

/// <summary>
/// Tests for UpdateProfileRequestValidator
/// Validates email format and username format/length
/// </summary>
public class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidUsernameAndEmail_ShouldPass()
    {
        var request = new UpdateProfileRequest
        {
            Username = "New Username",
            Email = "newemail@example.com"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidUsernameOnly_ShouldPass()
    {
        var request = new UpdateProfileRequest
        {
            Username = "New Name",
            Email = null
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidEmailOnly_ShouldPass()
    {
        var request = new UpdateProfileRequest
        {
            Username = null,
            Email = "newemail@example.com"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUsername_ShouldPass()
    {
        var request = new UpdateProfileRequest
        {
            Username = string.Empty,
            Email = "newemail@example.com"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ShouldFail()
    {
        var request = new UpdateProfileRequest
        {
            Username = "Valid Name",
            Email = "invalid-email"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithUsernameTooShort_ShouldFail()
    {
        var request = new UpdateProfileRequest
        {
            Username = "ab",
            Email = "valid@example.com"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithUsernameTooLong_ShouldFail()
    {
        var request = new UpdateProfileRequest
        {
            Username = new string('a', 51),
            Email = "valid@example.com"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithUsernameContainingNumbers_ShouldFail()
    {
        var request = new UpdateProfileRequest
        {
            Username = "User123",
            Email = "valid@example.com"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithUsernameContainingSpecialCharacters_ShouldFail()
    {
        var request = new UpdateProfileRequest
        {
            Username = "User_Name",
            Email = "valid@example.com"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }
}
