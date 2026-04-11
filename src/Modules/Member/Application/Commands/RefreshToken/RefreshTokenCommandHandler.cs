using Member.Application.Abstractions;
using Member.Application.DTOs;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Member.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenCommandResult>
{
    private readonly MemberDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        MemberDbContext db,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<RefreshTokenCommandResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(command.RawToken);

        var existingToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null || !existingToken.IsValid())
        {
            _logger.LogWarning("Invalid or expired refresh token attempt");
            throw new InvalidOperationException("INVALID_REFRESH_TOKEN");
        }

        var user = await _db.Users.FindAsync([existingToken.UserId], cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Refresh token for non-existent user {UserId}", existingToken.UserId);
            throw new InvalidOperationException("INVALID_REFRESH_TOKEN");
        }

        existingToken.Revoke();

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var (rawToken, newTokenHash, expiresAt) = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = Domain.RefreshToken.Create(user.Id, newTokenHash, expiresAt);

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token rotation for user {UserId}", user.Id);

        return new RefreshTokenCommandResult(new TokenDto(accessToken, rawToken, 900));
    }
}
