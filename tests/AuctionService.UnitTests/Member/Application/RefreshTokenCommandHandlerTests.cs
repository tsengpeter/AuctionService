using AuctionService.UnitTests.Member;
using FluentAssertions;
using Member.Application.Abstractions;
using Member.Application.Commands.RefreshToken;
using Member.Domain;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AuctionService.UnitTests.Member.Application;

public class RefreshTokenCommandHandlerTests
{
    private static (RefreshTokenCommandHandler Handler, MemberDbContext Db, IJwtTokenService Jwt) CreateSut()
    {
        var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var jwtService = Substitute.For<IJwtTokenService>();
        jwtService.HashToken(Arg.Any<string>()).Returns(x => $"hash_{x.Arg<string>()}");
        jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<MemberRole>())
            .Returns("new_access_token");
        jwtService.GenerateRefreshToken().Returns(("new_raw", "new_hash", DateTimeOffset.UtcNow.AddDays(7)));

        var handler = new RefreshTokenCommandHandler(db, jwtService, NullLogger<RefreshTokenCommandHandler>.Instance);
        return (handler, db, jwtService);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldReturnNewTokens_AndRevokeOld()
    {
        var (handler, db, jwtService) = CreateSut();

        var user = MemberUser.Create("alice@example.com", "alice1", "hash", 1, "912345678");
        db.Users.Add(user);

        var rawToken = "validtoken123";
        var tokenHash = $"hash_{rawToken}";
        var refreshToken = RefreshToken.Create(user.Id, tokenHash, DateTimeOffset.UtcNow.AddDays(7));
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.Tokens.AccessToken.Should().Be("new_access_token");
        refreshToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ShouldThrow()
    {
        var (handler, db, _) = CreateSut();

        var user = MemberUser.Create("bob@example.com", "bob1", "hash", 1, "912345678");
        db.Users.Add(user);

        var rawToken = "expiredtoken";
        var tokenHash = $"hash_{rawToken}";
        var refreshToken = RefreshToken.Create(user.Id, tokenHash, DateTimeOffset.UtcNow.AddDays(-1));
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var act = async () => await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INVALID_REFRESH_TOKEN*");
    }

    [Fact]
    public async Task Handle_RevokedToken_ShouldThrow()
    {
        var (handler, db, _) = CreateSut();

        var user = MemberUser.Create("charlie@example.com", "charlie1", "hash", 1, "912345678");
        db.Users.Add(user);

        var rawToken = "revokedtoken";
        var tokenHash = $"hash_{rawToken}";
        var refreshToken = RefreshToken.Create(user.Id, tokenHash, DateTimeOffset.UtcNow.AddDays(7));
        refreshToken.Revoke();
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var act = async () => await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INVALID_REFRESH_TOKEN*");
    }
}
