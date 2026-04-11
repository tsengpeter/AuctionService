using AuctionService.UnitTests.Member;
using FluentAssertions;
using FluentValidation;
using Member.Application.Abstractions;
using Member.Application.Commands.ChangePassword;
using Member.Domain;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AuctionService.UnitTests.Member.Application;

public class ChangePasswordCommandHandlerTests
{
    private static (ChangePasswordCommandHandler Handler, MemberDbContext Db, IPasswordHasher Hasher) CreateSut()
    {
        var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var hasher = Substitute.For<IPasswordHasher>();
        var handler = new ChangePasswordCommandHandler(db, hasher, NullLogger<ChangePasswordCommandHandler>.Instance);
        return (handler, db, hasher);
    }

    [Fact]
    public async Task Handle_ValidChange_ShouldUpdatePasswordAndRevokeTokens()
    {
        var (handler, db, hasher) = CreateSut();
        hasher.Verify("OldPass1!", "oldhash").Returns(true);
        hasher.Verify("NewPass1!", "oldhash").Returns(false);
        hasher.Hash("NewPass1!").Returns("newhash");

        var user = MemberUser.Create("alice@example.com", "alice1", "oldhash", 1, "912345678");
        db.Users.Add(user);

        var token = RefreshToken.Create(user.Id, "tokenHash123", DateTimeOffset.UtcNow.AddDays(7));
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        await handler.Handle(new ChangePasswordCommand(user.Id, "OldPass1!", "NewPass1!"), CancellationToken.None);

        var updatedUser = db.Users.Find(user.Id);
        updatedUser!.PasswordHash.Should().Be("newhash");

        var updatedToken = db.RefreshTokens.Find(token.Id);
        updatedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ShouldThrow()
    {
        var (handler, db, hasher) = CreateSut();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var user = MemberUser.Create("bob@example.com", "bob1", "oldhash", 1, "912345678");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var act = async () => await handler.Handle(new ChangePasswordCommand(user.Id, "WrongPass1!", "NewPass1!"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INVALID_CURRENT_PASSWORD*");
    }

    [Fact]
    public async Task Handle_SameAsCurrentPassword_ShouldThrowValidationException()
    {
        var (handler, db, hasher) = CreateSut();
        hasher.Verify("OldPass1!", "oldhash").Returns(true); // current password matches
        hasher.Verify("OldPass1!", "oldhash").Returns(true); // new password also matches (same)

        var user = MemberUser.Create("charlie@example.com", "charlie1", "oldhash", 1, "912345678");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Set up: first Verify call (current) returns true, second (new == old check) returns true
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true, true); // both calls return true

        var act = async () => await handler.Handle(new ChangePasswordCommand(user.Id, "OldPass1!", "OldPass1!"), CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }
}
