using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionService.Infrastructure.Repositories;

/// <summary>
/// 商品追蹤 Repository 實作
/// </summary>
public class FollowRepository : Repository<Follow>, IFollowRepository
{
    public FollowRepository(AuctionDbContext context, ILogger<FollowRepository> logger) : base(context, logger)
    {
    }

    public async Task<Follow> AddAsync(Follow follow)
    {
        _dbSet.Add(follow);
        await _context.SaveChangesAsync();
        return follow;
    }

    public async Task RemoveAsync(string userId, Guid auctionId)
    {
        var follow = await _dbSet
            .FirstOrDefaultAsync(f => f.UserId == userId && f.AuctionId == auctionId);

        if (follow != null)
        {
            _dbSet.Remove(follow);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<(IEnumerable<Follow> Follows, int TotalCount)> GetByUserIdAsync(string userId, AuctionQueryParameters parameters)
    {
        var query = _dbSet
            .Where(f => f.UserId == userId)
            .Include(f => f.Auction)
            .AsNoTracking()
            .AsQueryable();

        // 排序
        query = parameters.SortBy switch
        {
            AuctionSortBy.CreatedAt => parameters.SortDirection == SortDirection.Descending
                ? query.OrderByDescending(f => f.CreatedAt)
                : query.OrderBy(f => f.CreatedAt),
            AuctionSortBy.EndTime => parameters.SortDirection == SortDirection.Descending
                ? query.OrderByDescending(f => f.Auction!.EndTime)
                : query.OrderBy(f => f.Auction!.EndTime),
            _ => query.OrderByDescending(f => f.CreatedAt)
        };

        // 取得總筆數
        var totalCount = await query.CountAsync();

        // 分頁
        var follows = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return (follows, totalCount);
    }

    public async Task<bool> ExistsAsync(string userId, Guid auctionId)
    {
        return await _dbSet
            .AnyAsync(f => f.UserId == userId && f.AuctionId == auctionId);
    }

    public async Task<bool> IsFollowingOwnAuctionAsync(string userId, Guid auctionId)
    {
        return await _dbSet
            .AnyAsync(f => f.UserId == userId && f.AuctionId == auctionId &&
                          f.Auction != null && f.Auction.UserId == userId);
    }
}