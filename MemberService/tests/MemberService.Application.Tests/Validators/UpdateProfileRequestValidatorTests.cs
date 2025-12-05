using FluentAssertions;
using FluentValidation.TestHelper;
using MemberService.Application.DTOs.Users;
using MemberService.Application.Validators;
using Xunit;

namespace MemberService.Application.Tests.Validators;

public class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Username_Is_Valid()
    {
        // Arrange
        var request = new UpdateProfileRequest { Username = "張三豐" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Email_Is_Valid()
    {
        // Arrange
        var request = new UpdateProfileRequest { Email = "user@example.com" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Both_Username_And_Email_Are_Valid()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            Username = "李小明",
            Email = "newemail@example.com"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Request_Is_Empty()
    {
        // Arrange
        var request = new UpdateProfileRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Pass_When_Username_Is_Null_Or_Empty_Or_Whitespace(string? username)
    {
        // Arrange
        var request = new UpdateProfileRequest { Username = username };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Username_Is_Too_Short()
    {
        // Arrange
        var request = new UpdateProfileRequest { Username = "張" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters long.");
    }

    [Fact]
    public void Should_Fail_When_Username_Is_Too_Long()
    {
        // Arrange
        var request = new UpdateProfileRequest { Username = new string('張', 51) };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must not exceed 50 characters.");
    }

    [Fact]
    public void Should_Fail_When_Username_Contains_Invalid_Characters()
    {
        // Arrange
        var request = new UpdateProfileRequest { Username = "張三123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters and spaces.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Pass_When_Email_Is_Null_Or_Empty_Or_Whitespace(string? email)
    {
        // Arrange
        var request = new UpdateProfileRequest { Email = email };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user.example.com")]
    public void Should_Fail_When_Email_Is_Invalid_Format(string email)
    {
        // Arrange
        var request = new UpdateProfileRequest { Email = email };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Too_Long()
    {
        // Arrange
        var email = new string('a', 246) + "@example.com"; // 255 chars total
        var request = new UpdateProfileRequest { Email = email };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 255 characters.");
    }
}