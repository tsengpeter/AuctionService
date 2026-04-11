using FluentAssertions;
using Member.Domain;

namespace AuctionService.UnitTests.Member.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var hash = "tokenHash";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var token = RefreshToken.Create(userId, hash, expiresAt);

        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be(hash);
        token.ExpiresAt.Should().Be(expiresAt);
        token.IsRevoked.Should().BeFalse();
        token.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldSetCreatedAtAndUpdatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        token.CreatedAt.Should().BeOnOrAfter(before);
        token.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Revoke_ShouldSetIsRevoked_ToTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        token.Revoke();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Revoke_ShouldUpdateUpdatedAt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        var before = token.UpdatedAt;
        token.Revoke();
        token.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenNotRevokedAndNotExpired()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        token.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenRevoked()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        token.Revoke();
        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenExpired()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(-1));
        token.IsValid().Should().BeFalse();
    }
}
