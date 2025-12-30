using FluentValidation.TestHelper;
using MemberService.Application.DTOs.Auth;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator;

    public RefreshTokenRequestValidatorTests()
    {
        _validator = new RefreshTokenRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new RefreshTokenRequest("valid_refresh_token_string");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RefreshTokenIsEmpty_ShouldFail(string? refreshToken)
    {
        // Arrange
        var request = new RefreshTokenRequest(refreshToken!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token is required");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("a")]
    public void Validate_RefreshTokenTooShort_ShouldFail(string refreshToken)
    {
        // Arrange
        var request = new RefreshTokenRequest(refreshToken);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token must be at least 10 characters long");
    }
}