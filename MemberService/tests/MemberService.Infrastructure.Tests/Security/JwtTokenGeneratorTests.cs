using FluentAssertions;
using MemberService.Domain.Interfaces;
using MemberService.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace MemberService.Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests
{
    private const string TestSecretKey = "super-secret-key-for-testing-purposes-only";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";
    private const int AccessTokenExpirationMinutes = 15;
    private const int RefreshTokenExpirationDays = 7;

    private readonly JwtTokenGenerator _tokenGenerator;

    public JwtTokenGeneratorTests()
    {
        _tokenGenerator = new JwtTokenGenerator(TestSecretKey, TestIssuer, TestAudience, AccessTokenExpirationMinutes, RefreshTokenExpirationDays);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var userId = 12345L;
        var email = "test@example.com";

        // Act
        var token = _tokenGenerator.GenerateAccessToken(userId, email);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(TestIssuer);
        jwtToken.Audiences.Should().Contain(TestAudience);
        jwtToken.Subject.Should().Be(userId.ToString());

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim.Value.Should().Be(email);

        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jtiClaim.Should().NotBeNull();
        jtiClaim.Value.Should().NotBeNullOrEmpty();

        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveUniqueJtiForEachToken()
    {
        // Arrange
        var userId = 12345L;
        var email = "test@example.com";

        // Act
        var token1 = _tokenGenerator.GenerateAccessToken(userId, email);
        var token2 = _tokenGenerator.GenerateAccessToken(userId, email);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken1 = tokenHandler.ReadJwtToken(token1);
        var jwtToken2 = tokenHandler.ReadJwtToken(token2);

        var jti1 = jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

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
        var token1 = _tokenGenerator.GenerateRefreshToken();
        var token2 = _tokenGenerator.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultValues_WhenNotProvided()
    {
        // Arrange
        var generator = new JwtTokenGenerator(TestSecretKey);

        // Act
        var token = generator.GenerateAccessToken(1, "test@test.com");

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("MemberService");
        jwtToken.Audiences.Should().Contain("MemberService");
    }
}