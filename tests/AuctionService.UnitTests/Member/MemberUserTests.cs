using FluentAssertions;
using Member.Domain.Entities;

namespace AuctionService.UnitTests.Member;

public class MemberUserTests
{
    [Fact]
    public void Create_ShouldReturnMemberUser_WithCorrectProperties()
    {
        var user = MemberUser.Create("Test@Example.com", "testuser", "hash123");

        user.Email.Should().Be("test@example.com");
        user.Username.Should().Be("testuser");
        user.PasswordHash.Should().Be("hash123");
        user.Id.Should().NotBe(Guid.Empty);
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WhenEmailIsNullOrWhitespace_ShouldThrow(string? email)
    {
        var act = () => MemberUser.Create(email!, "user", "hash");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WhenPasswordHashIsNullOrWhitespace_ShouldThrow(string? hash)
    {
        var act = () => MemberUser.Create("test@example.com", "user", hash!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldNormalizeEmail_ToLowercase()
    {
        var user = MemberUser.Create("UPPER@EXAMPLE.COM", "user", "hash");
        user.Email.Should().Be("upper@example.com");
    }
}
