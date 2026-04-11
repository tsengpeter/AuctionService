using FluentAssertions;
using Member.Domain;
using Member.Domain.Entities;

namespace AuctionService.UnitTests.Member.Domain;

public class MemberUserTests
{
    private static MemberUser CreateDefault(
        string email = "alice@example.com",
        string username = "alice",
        string passwordHash = "hashedpassword",
        int phoneCountryCodeId = 1,
        string phoneNumber = "912345678") =>
        MemberUser.Create(email, username, passwordHash, phoneCountryCodeId, phoneNumber);

    [Fact]
    public void Create_ShouldSetEmailLowercase()
    {
        var user = CreateDefault(email: "Alice@Example.COM");
        user.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void Create_ShouldSetUsernameNormalized()
    {
        var user = CreateDefault(username: "Alice");
        user.UsernameNormalized.Should().Be("alice");
    }

    [Fact]
    public void Create_ShouldDefaultDisplayNameToUsername_WhenDisplayNameIsNull()
    {
        var user = MemberUser.Create("a@b.com", "alice", "hash", 1, "912345678", displayName: null);
        user.DisplayName.Should().Be("alice");
    }

    [Fact]
    public void Create_ShouldDefaultDisplayNameToUsername_WhenDisplayNameIsEmpty()
    {
        var user = MemberUser.Create("a@b.com", "alice", "hash", 1, "912345678", displayName: "");
        user.DisplayName.Should().Be("alice");
    }

    [Fact]
    public void Create_ShouldUseProvidedDisplayName()
    {
        var user = MemberUser.Create("a@b.com", "alice", "hash", 1, "912345678", displayName: "Alice Chen");
        user.DisplayName.Should().Be("Alice Chen");
    }

    [Fact]
    public void Create_ShouldStripLeadingZeroFromPhoneNumber_WhenStartingWith09()
    {
        var user = CreateDefault(phoneNumber: "0912345678");
        user.PhoneNumber.Should().Be("912345678");
    }

    [Fact]
    public void Create_ShouldNotModifyPhoneNumber_WhenNotStartingWith09()
    {
        var user = CreateDefault(phoneNumber: "912345678");
        user.PhoneNumber.Should().Be("912345678");
    }

    [Fact]
    public void Create_ShouldSetDefaultRole_ToMember()
    {
        var user = CreateDefault();
        user.Role.Should().Be(MemberRole.Member);
    }

    [Fact]
    public void Create_ShouldAssignNewGuid()
    {
        var user = CreateDefault();
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldSetCreatedAtAndUpdatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var user = CreateDefault();
        user.CreatedAt.Should().BeOnOrAfter(before);
        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateProfile_ShouldNormalizeUsername()
    {
        var user = CreateDefault();
        user.UpdateProfile("Bob", null, null, null, null, null);
        user.Username.Should().Be("Bob");
        user.UsernameNormalized.Should().Be("bob");
    }

    [Fact]
    public void UpdateProfile_ShouldDefaultDisplayNameToUsername_WhenNull()
    {
        var user = CreateDefault();
        user.UpdateProfile("bob", null, null, null, null, null);
        user.DisplayName.Should().Be("bob");
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateAddressFields()
    {
        var user = CreateDefault();
        user.UpdateProfile("alice", null, "Taiwan", "Taipei", "10001", "123 Main St");
        user.AddressCountry.Should().Be("Taiwan");
        user.AddressCity.Should().Be("Taipei");
        user.AddressPostalCode.Should().Be("10001");
        user.AddressLine.Should().Be("123 Main St");
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateUpdatedAt()
    {
        var user = CreateDefault();
        var before = user.UpdatedAt;
        user.UpdateProfile("alice", null, null, null, null, null);
        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ChangePassword_ShouldUpdatePasswordHash()
    {
        var user = CreateDefault();
        user.ChangePassword("newhashedpassword");
        user.PasswordHash.Should().Be("newhashedpassword");
    }

    [Fact]
    public void ChangePassword_ShouldUpdateUpdatedAt()
    {
        var user = CreateDefault();
        var before = user.UpdatedAt;
        user.ChangePassword("newhashedpassword");
        user.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
