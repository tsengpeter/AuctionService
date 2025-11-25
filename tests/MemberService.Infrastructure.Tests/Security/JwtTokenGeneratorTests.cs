using Xunit;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using MemberService.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace MemberService.Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        // Create in-memory configuration for testing
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:SecretKey", "test-secret-key-must-be-at-least-32-characters-long-for-security" },
                { "Jwt:Issuer", "member-service" },
                { "Jwt:Audience", "auction-api" },
                { "Jwt:ExpirationMinutes", "15" }
            })
            .Build();

        _configuration = configBuilder;
        _generator = new JwtTokenGenerator(_configuration);
    }

    [Fact]
    public void GenerateToken_WithValidInput_ReturnsValidJwt()
    {
        // Arrange
        var userId = 123456789L;
        var email = "test@example.com";

        // Act
        var token = _generator.GenerateToken(userId, email);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        // Verify JWT structure
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Should().NotBeNull();
        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == email);
    }

    [Fact]
    public void GenerateToken_TokenContainsCorrectClaims()
    {
        // Arrange
        var userId = 123456789L;
        var email = "test@example.com";

        // Act
        var token = _generator.GenerateToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("member-service");
        jwtToken.Audiences.Should().Contain("auction-api");
        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == email);
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectExpiration()
    {
        // Arrange
        var userId = 123456789L;
        var email = "test@example.com";
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _generator.GenerateToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Token should expire in approximately 15 minutes
        var expectedExpiry = beforeGeneration.AddMinutes(15);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_MultipleTokens_AreUnique()
    {
        // Arrange
        var userId = 123456789L;
        var email = "test@example.com";

        // Act
        var token1 = _generator.GenerateToken(userId, email);
        System.Threading.Thread.Sleep(100); // Ensure different timestamp
        var token2 = _generator.GenerateToken(userId, email);

        // Assert
        token1.Should().NotBe(token2);
    }
}
