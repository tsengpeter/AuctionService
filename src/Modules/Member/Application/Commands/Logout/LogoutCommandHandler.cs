using Member.Application.Abstractions;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Member.Application.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly MemberDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        MemberDbContext db,
        IJwtTokenService jwtTokenService,
        ILogger<LogoutCommandHandler> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(command.RawToken);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null)
        {
            _logger.LogInformation("Logout idempotent: token not found");
            return;
        }

        if (!token.IsRevoked)
        {
            token.Revoke();
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Token revoked for user {UserId}", token.UserId);
        }
        else
        {
            _logger.LogInformation("Logout idempotent: token already revoked for user {UserId}", token.UserId);
        }
    }
}
