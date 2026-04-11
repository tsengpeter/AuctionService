using AuctionService.UnitTests.Member;
using FluentAssertions;
using Member.Application.Abstractions;
using Member.Application.Commands.Logout;
using Member.Domain;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AuctionService.UnitTests.Member.Application;

public class LogoutCommandHandlerTests
{
    private static (LogoutCommandHandler Handler, MemberDbContext Db) CreateSut()
    {
        var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var jwtService = Substitute.For<IJwtTokenService>();
        jwtService.HashToken(Arg.Any<string>()).Returns(x => $"hash_{x.Arg<string>()}");
        var handler = new LogoutCommandHandler(db, jwtService, NullLogger<LogoutCommandHandler>.Instance);
        return (handler, db);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldRevokeToken()
    {
        var (handler, db) = CreateSut();

        var user = MemberUser.Create("alice@example.com", "alice1", "hash", 1, "912345678");
        db.Users.Add(user);

        var rawToken = "validtoken";
        var token = RefreshToken.Create(user.Id, $"hash_{rawToken}", DateTimeOffset.UtcNow.AddDays(7));
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        await handler.Handle(new LogoutCommand(rawToken), CancellationToken.None);

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ShouldNotThrow_Idempotent()
    {
        var (handler, db) = CreateSut();

        var user = MemberUser.Create("bob@example.com", "bob1", "hash", 1, "912345678");
        db.Users.Add(user);

        var rawToken = "revokedtoken";
        var token = RefreshToken.Create(user.Id, $"hash_{rawToken}", DateTimeOffset.UtcNow.AddDays(7));
        token.Revoke();
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        var act = async () => await handler.Handle(new LogoutCommand(rawToken), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_NonExistentToken_ShouldNotThrow_Idempotent()
    {
        var (handler, _) = CreateSut();

        var act = async () => await handler.Handle(new LogoutCommand("nonexistent"), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
