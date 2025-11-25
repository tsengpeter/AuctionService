using MemberService.Domain.Entities;

namespace MemberService.Domain.Interfaces;

/// <summary>
/// Repository interface for RefreshToken entity operations.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Finds a refresh token by its token value.
    /// </summary>
    /// <param name="token">The token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token if found, otherwise null</returns>
    Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new refresh token to the repository.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revokes all refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllForUserAsync(long userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes all expired refresh tokens from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveExpiredAsync(CancellationToken cancellationToken = default);
}
