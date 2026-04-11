using Member.Domain;

namespace Member.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, MemberRole role);
    (string RawToken, string TokenHash, DateTimeOffset ExpiresAt) GenerateRefreshToken();
    string HashToken(string rawToken);
}
