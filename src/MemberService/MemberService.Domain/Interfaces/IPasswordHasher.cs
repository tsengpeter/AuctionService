namespace MemberService.Domain.Interfaces;

/// <summary>
/// Interface for password hashing and verification operations.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password.
    /// </summary>
    /// <param name="password">The plaintext password</param>
    /// <param name="snowflakeId">The user's Snowflake ID (mixed with password for additional security)</param>
    /// <returns>The bcrypt password hash</returns>
    string HashPassword(string password, long snowflakeId);
    
    /// <summary>
    /// Verifies a plaintext password against a hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify</param>
    /// <param name="hash">The bcrypt hash to verify against</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hash);
}
