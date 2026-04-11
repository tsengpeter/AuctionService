using AuctionService.UnitTests.Member;
using FluentAssertions;
using Member.Application.Queries.GetMe;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionService.UnitTests.Member.Application;

public class GetMeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingUser_ShouldReturnUserDto()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var user = MemberUser.Create("alice@example.com", "alice1", "hash", 1, "912345678");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new GetMeQueryHandler(db, NullLogger<GetMeQueryHandler>.Instance);
        var result = await handler.Handle(new GetMeQuery(user.Id), CancellationToken.None);

        result.User.Id.Should().Be(user.Id);
        result.User.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrow()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var handler = new GetMeQueryHandler(db, NullLogger<GetMeQueryHandler>.Instance);

        var act = async () => await handler.Handle(new GetMeQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*USER_NOT_FOUND*");
    }
}
