using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.Security;

/// <summary>
/// Refresh token generator implementation
/// </summary>
public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a new refresh token
    /// </summary>
    /// <returns>A random refresh token string</returns>
    public string GenerateRefreshToken()
    {
        // Generate a random refresh token
        var randomBytes = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}