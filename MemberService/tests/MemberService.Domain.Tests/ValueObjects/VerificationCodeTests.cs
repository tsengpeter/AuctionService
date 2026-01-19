using MemberService.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace MemberService.Domain.Tests.ValueObjects;

public class VerificationCodeTests
{
    [Fact]
    public void Constructor_WithValidCode_CreatesVerificationCode()
    {
        // Arrange
        var code = "123456";
        var target = "test@example.com";

        // Act
        var verificationCode = new VerificationCode(code, target);

        // Assert
        verificationCode.Code.Should().Be(code);
        verificationCode.Target.Should().Be(target);
        verificationCode.AttemptCount.Should().Be(0);
        verificationCode.IsExpired.Should().BeFalse();
        verificationCode.IsMaxAttemptsExceeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("12345")]     // Too short
    [InlineData("1234567")]   // Too long
    [InlineData("12345a")]    // Contains letter
    [InlineData("")]          // Empty
    public void Constructor_WithInvalidCode_ThrowsArgumentException(string code)
    {
        // Arrange
        var target = "test@example.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new VerificationCode(code, target));
    }

    [Fact]
    public void IncrementAttempt_IncrementsCount()
    {
        // Arrange
        var verificationCode = new VerificationCode("123456", "test@example.com");

        // Act
        verificationCode.IncrementAttempt();

        // Assert
        verificationCode.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void IncrementAttempt_DoesNotExceedMax()
    {
        // Arrange
        var verificationCode = new VerificationCode("123456", "test@example.com");

        // Act
        for (int i = 0; i < 5; i++)
        {
            verificationCode.IncrementAttempt();
        }

        // Assert
        verificationCode.AttemptCount.Should().Be(3);
        verificationCode.IsMaxAttemptsExceeded.Should().BeTrue();
    }

    [Fact]
    public void SecondsUntilCanResend_ReturnsCorrectValue()
    {
        // Arrange
        var verificationCode = new VerificationCode("123456", "test@example.com");

        // Act
        var seconds = verificationCode.SecondsUntilCanResend;

        // Assert
        seconds.Should().BeLessThanOrEqualTo(60);
        seconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CanResend_IsFalseWithinCooldown()
    {
        // Arrange
        var verificationCode = new VerificationCode("123456", "test@example.com");

        // Act & Assert
        verificationCode.CanResend.Should().BeFalse();
    }
}
