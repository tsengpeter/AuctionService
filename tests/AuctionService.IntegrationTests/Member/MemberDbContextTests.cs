using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class MemberDbContextTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void DbContext_ShouldUseSchema_Member()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemberDbContext>();

        var model = db.Model;
        var entity = model.FindEntityType(typeof(MemberUser));

        entity!.GetSchema().Should().Be("member");
    }

    [Fact]
    public async Task DbContext_ShouldCreateAndReadMemberUser()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemberDbContext>();

        var user = MemberUser.Create(
            email: $"test_{Guid.NewGuid():N}@example.com",
            username: $"user{Guid.NewGuid().ToString("N")[..6]}",
            passwordHash: "hashedpassword",
            phoneCountryCodeId: 1,
            phoneNumber: "912345678");

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loaded = await db.Users.FindAsync(user.Id);
        loaded.Should().NotBeNull();
        loaded!.PhoneCountryCodeId.Should().Be(1);
    }

    [Fact]
    public async Task DbContext_ShouldEnforceEmailUniqueConstraint()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemberDbContext>();

        var email = $"unique_{Guid.NewGuid():N}@example.com";
        db.Users.Add(MemberUser.Create(email, "user1abc", "hash", 1, "912345678"));
        await db.SaveChangesAsync();

        var db2scope = fixture.Factory.Services.CreateScope();
        var db2 = db2scope.ServiceProvider.GetRequiredService<MemberDbContext>();
        db2.Users.Add(MemberUser.Create(email, "user2abc", "hash", 1, "912345678"));

        var act = async () => await db2.SaveChangesAsync();
        await act.Should().ThrowAsync<Exception>();
    }
}

