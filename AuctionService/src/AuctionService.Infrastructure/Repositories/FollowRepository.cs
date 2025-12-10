using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Infrastructure.Repositories;

/// <summary>
/// 商品追蹤 Repository 實作
/// </summary>
public class FollowRepository : Repository<Follow>, IFollowRepository
{
    public FollowRepository(AuctionDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Follow>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.UserId == userId)
            .Include(f => f.Auction)
                .ThenInclude(a => a!.Category)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsFollowingAsync(Guid userId, Guid auctionId)
    {
        return await _dbSet
            .AnyAsync(f => f.UserId == userId && f.AuctionId == auctionId);
    }

    public async Task<int> GetFollowCountAsync(Guid userId)
    {
        return await _dbSet
            .CountAsync(f => f.UserId == userId);
    }

    public async Task UnfollowAsync(Guid userId, Guid auctionId)
    {
        var follow = await _dbSet
            .FirstOrDefaultAsync(f => f.UserId == userId && f.AuctionId == auctionId);

        if (follow != null)
        {
            _dbSet.Remove(follow);
            await _context.SaveChangesAsync();
        }
    }
}