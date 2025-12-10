using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Infrastructure.Repositories;

/// <summary>
/// 商品分類 Repository 實作
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AuctionDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}