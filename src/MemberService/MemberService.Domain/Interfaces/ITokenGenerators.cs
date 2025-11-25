namespace MemberService.Domain.Interfaces;

/// <summary>
/// JWT token generator interface.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate a JWT access token.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The user email</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(long userId, string email);
}

/// <summary>
/// Refresh token generator interface.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Generate a refresh token.
    /// </summary>
    /// <returns>Refresh token string (Base64 encoded)</returns>
    string GenerateToken();
}
