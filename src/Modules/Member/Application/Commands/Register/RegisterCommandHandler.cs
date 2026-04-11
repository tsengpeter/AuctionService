using Member.Application.Abstractions;
using Member.Application.DTOs;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Member.Application.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterCommandResult>
{
    private readonly MemberDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        MemberDbContext db,
        IPasswordHasher passwordHasher,
        ILogger<RegisterCommandHandler> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<RegisterCommandResult> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var emailLower = command.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == emailLower, cancellationToken))
        {
            _logger.LogWarning("Registration conflict: email {Email} already registered", emailLower);
            throw new InvalidOperationException("EMAIL_CONFLICT");
        }

        var phoneCountryCode = await _db.PhoneCountryCodes
            .FindAsync([command.PhoneCountryCodeId], cancellationToken);

        if (phoneCountryCode == null)
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("PhoneCountryCodeId", "Invalid phone country code.")]);

        var passwordHash = _passwordHasher.Hash(command.Password);

        var user = MemberUser.Create(
            email: command.Email,
            username: command.Username,
            passwordHash: passwordHash,
            phoneCountryCodeId: command.PhoneCountryCodeId,
            phoneNumber: command.PhoneNumber,
            displayName: command.DisplayName,
            addressCountry: command.AddressCountry,
            addressCity: command.AddressCity,
            addressPostalCode: command.AddressPostalCode,
            addressLine: command.AddressLine);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        // Load navigation property for DTO mapping
        await _db.Entry(user).Reference(u => u.CountryCode).LoadAsync(cancellationToken);

        _logger.LogInformation("New user registered: {UserId}", user.Id);

        return new RegisterCommandResult(user.ToDto());
    }
}
