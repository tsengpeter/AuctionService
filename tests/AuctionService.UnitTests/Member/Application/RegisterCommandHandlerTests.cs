using AuctionService.UnitTests.Member;
using FluentAssertions;
using FluentValidation;
using Member.Application.Abstractions;
using Member.Application.Commands.Register;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AuctionService.UnitTests.Member.Application;

public class RegisterCommandHandlerTests
{
    private static RegisterCommand ValidCommand(string? email = null, string? username = null) =>
        new(
            Email: email ?? $"alice_{Guid.NewGuid():N}@example.com",
            Username: username ?? $"alice{Guid.NewGuid().ToString("N")[..6]}",
            Password: "Password1!",
            DisplayName: null,
            PhoneCountryCodeId: 1,
            PhoneNumber: "912345678",
            AddressCountry: null,
            AddressCity: null,
            AddressPostalCode: null,
            AddressLine: null);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateUser_AndReturnUserDto()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash(Arg.Any<string>()).Returns("hashedpassword");

        var handler = new RegisterCommandHandler(db, hasher, NullLogger<RegisterCommandHandler>.Instance);
        var command = ValidCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.User.Email.Should().Be(command.Email.ToLowerInvariant());
        result.User.Username.Should().Be(command.Username);
        result.User.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldThrowInvalidOperationException()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash(Arg.Any<string>()).Returns("hashedpassword");

        var handler = new RegisterCommandHandler(db, hasher, NullLogger<RegisterCommandHandler>.Instance);
        var command = ValidCommand(email: "duplicate@example.com");

        await handler.Handle(command, CancellationToken.None);

        // Try same email again
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EMAIL_CONFLICT*");
    }

    [Fact]
    public async Task Handle_InvalidPhoneCountryCodeId_ShouldThrowValidationException()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash(Arg.Any<string>()).Returns("hashedpassword");

        var handler = new RegisterCommandHandler(db, hasher, NullLogger<RegisterCommandHandler>.Instance);
        var command = ValidCommand() with { PhoneCountryCodeId = 999 };

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }
}

