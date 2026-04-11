using FluentAssertions;
using Member.Infrastructure.Services;

namespace AuctionService.UnitTests.Member.Infrastructure;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var result = _sut.Hash("SecretPassword1!");
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_SamePassword_ShouldProduceDifferentHashes_DueToSalt()
    {
        var hash1 = _sut.Hash("SamePassword1!");
        var hash2 = _sut.Hash("SamePassword1!");
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ShouldReturnTrue()
    {
        var hash = _sut.Hash("CorrectPassword1!");
        var result = _sut.Verify("CorrectPassword1!", hash);
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ShouldReturnFalse()
    {
        var hash = _sut.Hash("CorrectPassword1!");
        var result = _sut.Verify("WrongPassword1!", hash);
        result.Should().BeFalse();
    }
}
