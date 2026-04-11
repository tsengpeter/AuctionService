using AuctionService.UnitTests.Member;
using FluentAssertions;
using Member.Application.Commands.UpdateProfile;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionService.UnitTests.Member.Application;

public class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidUpdate_ShouldReturnUpdatedUser()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var user = MemberUser.Create("alice@example.com", "aliceold", "hash", 1, "912345678");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new UpdateProfileCommandHandler(db, NullLogger<UpdateProfileCommandHandler>.Instance);
        var command = new UpdateProfileCommand(user.Id, "alicenew", "Alice New", "Taiwan", "Taipei", "10001", "123 St");

        var result = await handler.Handle(command, CancellationToken.None);

        result.User.Username.Should().Be("alicenew");
        result.User.DisplayName.Should().Be("Alice New");
    }

    [Fact]
    public async Task Handle_UsernameConflict_ShouldThrow()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var user1 = MemberUser.Create("alice@example.com", "alice1", "hash", 1, "912345678");
        var user2 = MemberUser.Create("bob@example.com", "bob1", "hash", 1, "912345679");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var handler = new UpdateProfileCommandHandler(db, NullLogger<UpdateProfileCommandHandler>.Instance);
        var command = new UpdateProfileCommand(user2.Id, "alice1", null, null, null, null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*USERNAME_CONFLICT*");
    }
}
