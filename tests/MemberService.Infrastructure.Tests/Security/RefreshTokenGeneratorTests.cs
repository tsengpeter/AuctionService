using Xunit;
using FluentAssertions;
using MemberService.Infrastructure.Security;

namespace MemberService.Infrastructure.Tests.Security;

public class RefreshTokenGeneratorTests
{
    private readonly RefreshTokenGenerator _generator = new();

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        // Act
        var token = _generator.GenerateToken();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_ProducesUniqueTokens()
    {
        // Act
        var tokens = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            tokens.Add(_generator.GenerateToken());
        }

        // Assert
        tokens.Count.Should().Be(100);
    }

    [Fact]
    public void GenerateToken_TokenIsBase64()
    {
        // Arrange & Act
        var token = _generator.GenerateToken();

        // Assert
        // Should not throw when decoding base64
        var bytes = Convert.FromBase64String(token);
        bytes.Length.Should().Be(32); // 256 bits = 32 bytes
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectLength()
    {
        // Act
        var token = _generator.GenerateToken();

        // Assert
        // 32 bytes in base64 should be 44 characters (including padding)
        token.Length.Should().Be(44);
    }
}
