using Member.Application.DTOs;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Member.Application.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileCommandResult>
{
    private readonly MemberDbContext _db;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(MemberDbContext db, ILogger<UpdateProfileCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UpdateProfileCommandResult> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.CountryCode)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("USER_NOT_FOUND");

        var usernameNormalized = command.Username.Trim().ToLowerInvariant();
        var conflict = await _db.Users.AnyAsync(
            u => u.UsernameNormalized == usernameNormalized && u.Id != command.UserId,
            cancellationToken);

        if (conflict)
        {
            _logger.LogWarning("UpdateProfile conflict: username {Username} already taken", command.Username);
            throw new InvalidOperationException("USERNAME_CONFLICT");
        }

        user.UpdateProfile(
            command.Username,
            command.DisplayName,
            command.AddressCountry,
            command.AddressCity,
            command.AddressPostalCode,
            command.AddressLine);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Profile updated for user {UserId}", user.Id);

        return new UpdateProfileCommandResult(user.ToDto());
    }
}
