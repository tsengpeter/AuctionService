using AuctionService.Core.DTOs.Common;
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

    public async Task<IEnumerable<Auction>> GetByUserIdAsync(string userId)
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

    public async Task<(IEnumerable<Auction> Auctions, int TotalCount)> GetAuctionsAsync(AuctionQueryParameters parameters)
    {
        var query = _dbSet
            .Include(a => a.Category)
            .AsQueryable();

        // 搜尋關鍵字
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            query = query.Where(a => a.Name.Contains(parameters.SearchTerm) || a.Description.Contains(parameters.SearchTerm));
        }

        // 分類篩選
        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == parameters.CategoryId.Value);
        }

        // 狀態篩選
        if (parameters.Status.HasValue)
        {
            var now = DateTime.UtcNow;
            query = parameters.Status.Value switch
            {
                AuctionStatus.Pending => query.Where(a => a.StartTime > now),
                AuctionStatus.Active => query.Where(a => a.StartTime <= now && a.EndTime > now),
                AuctionStatus.Ended => query.Where(a => a.EndTime <= now),
                _ => query
            };
        }

        // 價格範圍篩選
        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(a => a.StartingPrice >= parameters.MinPrice.Value);
        }
        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(a => a.StartingPrice <= parameters.MaxPrice.Value);
        }

        // 排序
        query = (parameters.SortBy, parameters.SortDirection) switch
        {
            (AuctionSortBy.EndTime, SortDirection.Ascending) => query.OrderBy(a => a.EndTime),
            (AuctionSortBy.EndTime, SortDirection.Descending) => query.OrderByDescending(a => a.EndTime),
            (AuctionSortBy.StartTime, SortDirection.Ascending) => query.OrderBy(a => a.StartTime),
            (AuctionSortBy.StartTime, SortDirection.Descending) => query.OrderByDescending(a => a.StartTime),
            (AuctionSortBy.CurrentPrice, SortDirection.Ascending) => query.OrderBy(a => a.StartingPrice),
            (AuctionSortBy.CurrentPrice, SortDirection.Descending) => query.OrderByDescending(a => a.StartingPrice),
            (AuctionSortBy.Title, SortDirection.Ascending) => query.OrderBy(a => a.Name),
            (AuctionSortBy.Title, SortDirection.Descending) => query.OrderByDescending(a => a.Name),
            (AuctionSortBy.CreatedAt, SortDirection.Ascending) => query.OrderBy(a => a.CreatedAt),
            (AuctionSortBy.CreatedAt, SortDirection.Descending) => query.OrderByDescending(a => a.CreatedAt),
            _ => query.OrderBy(a => a.EndTime)
        };

        // 取得總筆數
        var totalCount = await query.CountAsync();

        // 分頁
        var auctions = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return (auctions, totalCount);
    }

    public async Task<Auction?> GetAuctionByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<(IEnumerable<Auction> Auctions, int TotalCount)> GetByUserIdAsync(string userId, AuctionQueryParameters parameters)
    {
        var query = _dbSet
            .Where(a => a.UserId == userId)
            .Include(a => a.Category);

        var totalCount = await query.CountAsync();

        var auctions = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return (auctions, totalCount);
    }

    public async Task<Auction> CreateAsync(Auction auction)
    {
        _dbSet.Add(auction);
        await _context.SaveChangesAsync();

        // 重新載入包含 Category 的實體
        return await _dbSet
            .Include(a => a.Category)
            .FirstAsync(a => a.Id == auction.Id);
    }

    public async Task<Auction> UpdateAsync(Auction auction)
    {
        _dbSet.Update(auction);
        await _context.SaveChangesAsync();
        return auction;
    }

    public async Task DeleteAsync(Guid id)
    {
        var auction = await _dbSet.FindAsync(id);
        if (auction != null)
        {
            _dbSet.Remove(auction);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsOwnerAsync(Guid auctionId, string userId)
    {
        var auction = await _dbSet.FindAsync(auctionId);
        return auction?.UserId == userId;
    }
}