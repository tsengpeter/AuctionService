using FluentValidation;
using FluentValidation.Results;
using Member.Application.Abstractions;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Member.Application.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly MemberDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        MemberDbContext db,
        IPasswordHasher passwordHasher,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([command.UserId], cancellationToken);

        if (user == null)
            throw new InvalidOperationException("USER_NOT_FOUND");

        if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("ChangePassword: wrong current password for user {UserId}", command.UserId);
            throw new InvalidOperationException("INVALID_CURRENT_PASSWORD");
        }

        if (_passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            throw new ValidationException(
                [new ValidationFailure("NewPassword", "New password must be different from the current password.")]);
        }

        var newHash = _passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newHash);

        // Revoke all refresh tokens for this user
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == command.UserId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
            token.Revoke();

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}, {Count} refresh tokens revoked",
            command.UserId, tokens.Count);
    }
}
