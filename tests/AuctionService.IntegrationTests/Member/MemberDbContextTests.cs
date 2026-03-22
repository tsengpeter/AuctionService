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
            $"test_{Guid.NewGuid():N}@example.com",
            "testuser",
            "hashedpassword");

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loaded = await db.Users.FindAsync(user.Id);
        loaded.Should().NotBeNull();
        loaded!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task DbContext_ShouldEnforceEmailUniqueConstraint()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemberDbContext>();

        var email = $"unique_{Guid.NewGuid():N}@example.com";
        db.Users.Add(MemberUser.Create(email, "user1", "hash"));
        await db.SaveChangesAsync();

        var db2scope = fixture.Factory.Services.CreateScope();
        var db2 = db2scope.ServiceProvider.GetRequiredService<MemberDbContext>();
        db2.Users.Add(MemberUser.Create(email, "user2", "hash"));

        var act = async () => await db2.SaveChangesAsync();
        await act.Should().ThrowAsync<Exception>();
    }
}
