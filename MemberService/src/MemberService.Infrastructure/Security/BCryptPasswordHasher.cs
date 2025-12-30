using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.Security;

/// <summary>
/// BCrypt password hasher implementation
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password, long userId)
    {
        // Combine password with snowflake ID for additional security
        string combined = password + userId.ToString();

        // Hash using BCrypt
        return BCrypt.Net.BCrypt.HashPassword(combined, WorkFactor);
    }

    public bool VerifyPassword(string password, long userId, string hash)
    {
        try
        {
            string combined = password + userId.ToString();
            return BCrypt.Net.BCrypt.Verify(combined, hash);
        }
        catch
        {
            // Invalid hash format
            return false;
        }
    }
}