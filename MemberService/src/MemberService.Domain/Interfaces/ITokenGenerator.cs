namespace MemberService.Domain.Interfaces;

/// <summary>
/// Token generator interface
/// </summary>
public interface ITokenGenerator
{
    string GenerateAccessToken(long userId, string email);
    string GenerateRefreshToken();
}