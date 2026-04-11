using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Member.Domain;
using Member.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace AuctionService.UnitTests.Member.Infrastructure;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SECRET"] = "test_jwt_secret_at_least_32_chars_long_for_tests",
                ["JWT_ISSUER"] = "AuctionService",
                ["JWT_AUDIENCE"] = "AuctionService"
            })
            .Build();
        _sut = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainSubClaim()
    {
        var userId = Guid.NewGuid();
        var token = _sut.GenerateAccessToken(userId, "alice@example.com", MemberRole.Member);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainEmailClaim()
    {
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "alice@example.com", MemberRole.Member);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "alice@example.com");
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainRoleClaim()
    {
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "alice@example.com", MemberRole.Member);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        parsed.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role ||
            c.Type == "role" ||
            c.Value == "Member");
    }

    [Fact]
    public void GenerateAccessToken_ShouldExpireIn15Minutes()
    {
        var before = DateTime.UtcNow;
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "alice@example.com", MemberRole.Member);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        parsed.ValidTo.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void HashToken_ShouldReturnConsistentHash_ForSameInput()
    {
        var raw = "someRawToken";
        var hash1 = _sut.HashToken(raw);
        var hash2 = _sut.HashToken(raw);
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyRawToken()
    {
        var (rawToken, _, _) = _sut.GenerateRefreshToken();
        rawToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyHash()
    {
        var (_, tokenHash, _) = _sut.GenerateRefreshToken();
        tokenHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldExpireIn7Days()
    {
        var before = DateTimeOffset.UtcNow;
        var (_, _, expiresAt) = _sut.GenerateRefreshToken();
        expiresAt.Should().BeCloseTo(before.AddDays(7), TimeSpan.FromSeconds(10));
    }
}
