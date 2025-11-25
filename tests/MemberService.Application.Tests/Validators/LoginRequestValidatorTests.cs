using FluentAssertions;
using MemberService.Application.DTOs.Auth;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

/// <summary>
/// Unit tests for LoginRequestValidator.
/// </summary>
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "ValidPassword123!"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyEmail_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "ValidPassword123!"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_InvalidEmailFormat_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "not-an-email",
            Password = "ValidPassword123!"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EmptyPassword_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
