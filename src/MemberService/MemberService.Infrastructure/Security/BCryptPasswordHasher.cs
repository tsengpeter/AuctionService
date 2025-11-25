using BCrypt.Net;
using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.Security;

/// <summary>
/// BCrypt password hasher implementation.
/// Uses bcrypt with snowflake ID mixing for additional security depth.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // Cost factor for bcrypt (slower = more secure)
    
    /// <summary>
    /// Hashes a password using bcrypt with snowflake ID mixing.
    /// </summary>
    /// <param name="password">The plaintext password</param>
    /// <param name="snowflakeId">The user's snowflake ID for additional entropy</param>
    /// <returns>The bcrypt hash string</returns>
    public string HashPassword(string password, long snowflakeId)
    {
        // Mix snowflake ID with password for additional security depth
        var combinedInput = $"{password}|{snowflakeId}";
        
        // Use bcrypt with the specified work factor
        return BCrypt.Net.BCrypt.HashPassword(combinedInput, workFactor: WorkFactor);
    }
    
    /// <summary>
    /// Verifies a plaintext password against a bcrypt hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify</param>
    /// <param name="hash">The bcrypt hash to verify against</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;
        
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // If hash is invalid or comparison fails, return false
            return false;
        }
    }
}
