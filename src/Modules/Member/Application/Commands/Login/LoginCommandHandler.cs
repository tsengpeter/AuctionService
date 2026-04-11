using Member.Application.Abstractions;
using Member.Application.DTOs;
using Member.Domain;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Member.Application.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginCommandResult>
{
    private readonly MemberDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LoginCommandHandler> _logger;

    private const int MaxFailures = 5;
    private static string CacheKey(string? ip) => $"login_fail:{ip ?? "unknown"}";

    public LoginCommandHandler(
        MemberDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IMemoryCache cache,
        ILogger<LoginCommandHandler> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LoginCommandResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(command.ClientIp);
        var failCount = _cache.TryGetValue<int>(cacheKey, out var count) ? count : 0;

        if (failCount >= MaxFailures)
        {
            _logger.LogWarning("Login blocked due to too many failures from IP {ClientIp}", command.ClientIp);
            throw new InvalidOperationException("TOO_MANY_ATTEMPTS");
        }

        var emailLower = command.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailLower, cancellationToken);

        if (user == null || !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            var newCount = failCount + 1;
            _cache.Set(cacheKey, newCount, TimeSpan.FromMinutes(1));
            _logger.LogWarning("Login failed for IP {ClientIp}, failure count: {Count}", command.ClientIp, newCount);
            throw new InvalidOperationException("INVALID_CREDENTIALS");
        }

        _cache.Remove(cacheKey);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var (rawToken, tokenHash, expiresAt) = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = Domain.RefreshToken.Create(user.Id, tokenHash, expiresAt);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in from IP {ClientIp}", user.Id, command.ClientIp);

        return new LoginCommandResult(new TokenDto(accessToken, rawToken, 900));
    }
}
