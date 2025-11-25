using System.Security.Cryptography;
using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.Security;

/// <summary>
/// Refresh token generator.
/// Generates cryptographically secure 256-bit random tokens encoded as Base64.
/// </summary>
public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeBytes = 32; // 256 bits
    
    /// <summary>
    /// Generates a new refresh token.
    /// </summary>
    /// <returns>Base64-encoded random token string</returns>
    public string GenerateToken()
    {
        var randomBytes = new byte[TokenSizeBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}
