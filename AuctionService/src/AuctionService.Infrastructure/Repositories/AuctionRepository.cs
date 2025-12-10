using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Infrastructure.Repositories;

/// <summary>
/// 拍賣商品 Repository 實作
/// </summary>
public class AuctionRepository : Repository<Auction>, IAuctionRepository
{
    public AuctionRepository(AuctionDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Auction>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .Include(a => a.Category)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Auction>> GetByCategoryIdAsync(int categoryId)
    {
        return await _dbSet
            .Where(a => a.CategoryId == categoryId)
            .Include(a => a.Category)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Auction>> GetActiveAuctionsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(a => a.StartTime <= now && a.EndTime > now)
            .Include(a => a.Category)
            .OrderBy(a => a.EndTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Auction>> GetEndingSoonAsync(int hours = 24)
    {
        var now = DateTime.UtcNow;
        var endTime = now.AddHours(hours);
        return await _dbSet
            .Where(a => a.EndTime > now && a.EndTime <= endTime)
            .Include(a => a.Category)
            .OrderBy(a => a.EndTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Auction>> SearchAsync(string searchTerm, int? categoryId = null)
    {
        var query = _dbSet
            .Include(a => a.Category)
            .Where(a => a.Name.Contains(searchTerm) || a.Description.Contains(searchTerm));

        if (categoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == categoryId.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsOwnerAsync(Guid auctionId, Guid userId)
    {
        return await _dbSet
            .AnyAsync(a => a.Id == auctionId && a.UserId == userId);
    }
}