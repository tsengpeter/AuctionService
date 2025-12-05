namespace MemberService.Domain.Interfaces;

/// <summary>
/// Refresh token generator interface
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a new refresh token
    /// </summary>
    /// <returns>A random refresh token string</returns>
    string GenerateRefreshToken();
}