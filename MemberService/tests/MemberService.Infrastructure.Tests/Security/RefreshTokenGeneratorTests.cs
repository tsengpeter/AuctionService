using FluentAssertions;
using MemberService.Infrastructure.Security;
using Xunit;

namespace MemberService.Infrastructure.Tests.Security;

public class RefreshTokenGeneratorTests
{
    private readonly RefreshTokenGenerator _refreshTokenGenerator;

    public RefreshTokenGeneratorTests()
    {
        _refreshTokenGenerator = new RefreshTokenGenerator();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _refreshTokenGenerator.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();

        // Should be valid base64
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
    {
        // Act
        var token1 = _refreshTokenGenerator.GenerateRefreshToken();
        var token2 = _refreshTokenGenerator.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }
}