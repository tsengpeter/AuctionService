using AuctionService.UnitTests.Member;
using FluentAssertions;
using Member.Application.Abstractions;
using Member.Application.Commands.Login;
using Member.Application.DTOs;
using Member.Domain;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using Member.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AuctionService.UnitTests.Member.Application;

public class LoginCommandHandlerTests
{
    private static (LoginCommandHandler Handler, MemberDbContext Db) CreateSut()
    {
        var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        var sp = serviceCollection.BuildServiceProvider();
        var cache = sp.GetRequiredService<IMemoryCache>();

        hasher.Hash(Arg.Any<string>()).Returns("hashed");
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<MemberRole>())
            .Returns("access_token");
        jwtService.GenerateRefreshToken().Returns(("rawToken", "hash123", DateTimeOffset.UtcNow.AddDays(7)));

        var handler = new LoginCommandHandler(db, hasher, jwtService, cache, NullLogger<LoginCommandHandler>.Instance);
        return (handler, db);
    }

    private static async Task SeedUser(MemberDbContext db, string email, string password)
    {
        var hasher = new BcryptPasswordHasher();
        var user = MemberUser.Create(email, "user123", hasher.Hash(password), 1, "912345678");
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnTokens()
    {
        var (handler, db) = CreateSut();
        await SeedUser(db, "alice@example.com", "Password1!");

        var hasher = new BcryptPasswordHasher();
        var realHash = hasher.Hash("Password1!");
        var user = db.Users.First();
        db.Users.Attach(user);
        // Re-seed with real hash
        using var db2 = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        db2.Users.Add(MemberUser.Create("alice2@example.com", "alice2", realHash, 1, "912345678"));
        await db2.SaveChangesAsync();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        var sp = serviceCollection.BuildServiceProvider();
        var cache = sp.GetRequiredService<IMemoryCache>();

        var hasher2 = Substitute.For<IPasswordHasher>();
        hasher2.Verify("Password1!", Arg.Any<string>()).Returns(true);
        var jwtService = Substitute.For<IJwtTokenService>();
        jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<MemberRole>())
            .Returns("access_token");
        jwtService.GenerateRefreshToken().Returns(("rawToken", "tokenHash123", DateTimeOffset.UtcNow.AddDays(7)));

        var handler2 = new LoginCommandHandler(db2, hasher2, jwtService, cache, NullLogger<LoginCommandHandler>.Instance);

        var result = await handler2.Handle(new LoginCommand("alice2@example.com", "Password1!", "127.0.0.1"), CancellationToken.None);
        result.Tokens.AccessToken.Should().Be("access_token");
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldThrow()
    {
        var (handler, db) = CreateSut();

        using var db2 = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        db2.Users.Add(MemberUser.Create("bob@example.com", "bob123", "hashed", 1, "912345678"));
        await db2.SaveChangesAsync();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        var sp = serviceCollection.BuildServiceProvider();
        var cache = sp.GetRequiredService<IMemoryCache>();

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var jwtService = Substitute.For<IJwtTokenService>();

        var h = new LoginCommandHandler(db2, hasher, jwtService, cache, NullLogger<LoginCommandHandler>.Instance);

        var act = async () => await h.Handle(new LoginCommand("bob@example.com", "wrong", "127.0.0.1"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INVALID_CREDENTIALS*");
    }

    [Fact]
    public async Task Handle_UnknownEmail_ShouldThrow()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        var sp = serviceCollection.BuildServiceProvider();
        var cache = sp.GetRequiredService<IMemoryCache>();

        var hasher = Substitute.For<IPasswordHasher>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var h = new LoginCommandHandler(db, hasher, jwtService, cache, NullLogger<LoginCommandHandler>.Instance);

        var act = async () => await h.Handle(new LoginCommand("none@example.com", "Password1!", "127.0.0.1"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INVALID_CREDENTIALS*");
    }

    [Fact]
    public async Task Handle_TooManyFailures_ShouldThrow()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        db.Users.Add(MemberUser.Create("charlie@example.com", "charlie1", "hashed", 1, "912345678"));
        await db.SaveChangesAsync();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        var sp = serviceCollection.BuildServiceProvider();
        var cache = sp.GetRequiredService<IMemoryCache>();

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var jwtService = Substitute.For<IJwtTokenService>();
        var h = new LoginCommandHandler(db, hasher, jwtService, cache, NullLogger<LoginCommandHandler>.Instance);

        // Make 5 failures
        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => h.Handle(new LoginCommand("charlie@example.com", "wrong", "10.0.0.1"), CancellationToken.None));
        }

        // 6th attempt should be blocked
        var act = async () => await h.Handle(new LoginCommand("charlie@example.com", "wrong", "10.0.0.1"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TOO_MANY_ATTEMPTS*");
    }
}
