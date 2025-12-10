using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Infrastructure.Repositories;

/// <summary>
/// API 回應代碼 Repository 實作
/// </summary>
public class ResponseCodeRepository : Repository<ResponseCode>, IResponseCodeRepository
{
    public ResponseCodeRepository(AuctionDbContext context) : base(context)
    {
    }

    public async Task<ResponseCode?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rc => rc.Code == code);
    }

    public async Task<IEnumerable<ResponseCode>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(rc => rc.Category == category)
            .OrderBy(rc => rc.Code)
            .ToListAsync();
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        return await _dbSet
            .AnyAsync(rc => rc.Code == code);
    }
}