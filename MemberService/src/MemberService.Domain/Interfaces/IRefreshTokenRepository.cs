using MemberService.Domain.Entities;

namespace MemberService.Domain.Interfaces;

/// <summary>
/// RefreshToken repository interface
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetValidTokensByUserIdAsync(long userId);
    Task AddAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAllUserTokensAsync(long userId);
}