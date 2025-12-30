namespace MemberService.Domain.Interfaces;

/// <summary>
/// Password hasher interface
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password, long userId);
    bool VerifyPassword(string password, long userId, string hash);
}