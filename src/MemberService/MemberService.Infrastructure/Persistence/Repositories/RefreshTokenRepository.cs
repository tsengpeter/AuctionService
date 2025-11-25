using Microsoft.EntityFrameworkCore;
using MemberService.Domain.Entities;
using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for RefreshToken entity.
/// Provides persistence operations for refresh token management.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly MemberDbContext _context;
    
    public RefreshTokenRepository(MemberDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    public async Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }
    
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (refreshToken == null)
            throw new ArgumentNullException(nameof(refreshToken));
        
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task RevokeAllForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        
        foreach (var token in tokens)
        {
            token.Revoke();
        }
        
        if (tokens.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    
    public async Task RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        
        if (expiredTokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
