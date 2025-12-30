using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionService.Infrastructure.Repositories;

/// <summary>
/// 通用 Repository 實作
/// </summary>
/// <typeparam name="T">實體類型</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AuctionDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    private const int PerformanceThresholdMs = 1000; // 效能閾值：1000ms

    public Repository(AuctionDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await ExecuteWithPerformanceLogging(
            () => _dbSet.ToListAsync(),
            nameof(GetAllAsync));
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await ExecuteWithPerformanceLogging(
            () => _dbSet.FindAsync(id).AsTask(),
            $"{nameof(GetByIdAsync)}({id})");
    }

    public async Task<T> AddAsync(T entity)
    {
        return await ExecuteWithPerformanceLogging(
            async () =>
            {
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            },
            $"{nameof(AddAsync)}({typeof(T).Name})");
    }

    public async Task UpdateAsync(T entity)
    {
        await ExecuteWithPerformanceLogging(
            async () =>
            {
                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
            },
            $"{nameof(UpdateAsync)}({typeof(T).Name})");
    }

    public async Task DeleteAsync(T entity)
    {
        await ExecuteWithPerformanceLogging(
            async () =>
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            },
            $"{nameof(DeleteAsync)}({typeof(T).Name})");
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await ExecuteWithPerformanceLogging(
            () => _dbSet.FindAsync(id).AsTask().ContinueWith(t => t.Result != null),
            $"{nameof(ExistsAsync)}({id})");
    }

    /// <summary>
    /// 執行操作並記錄效能資訊
    /// </summary>
    private async Task ExecuteWithPerformanceLogging(Func<Task> operation, string operationName)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await operation();
        }
        finally
        {
            stopwatch.Stop();
            LogPerformance(operationName, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 執行操作並記錄效能資訊（有回傳值）
    /// </summary>
    private async Task<TResult> ExecuteWithPerformanceLogging<TResult>(Func<Task<TResult>> operation, string operationName)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await operation();
            return result;
        }
        finally
        {
            stopwatch.Stop();
            LogPerformance(operationName, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 記錄效能資訊
    /// </summary>
    private void LogPerformance(string operationName, long elapsedMs)
    {
        if (elapsedMs > PerformanceThresholdMs)
        {
            _logger.LogWarning(
                "慢查詢檢測: {OperationName} 執行時間 {ElapsedMs}ms 超過閾值 {Threshold}ms",
                operationName,
                elapsedMs,
                PerformanceThresholdMs);
        }
        else
        {
            _logger.LogDebug(
                "查詢效能: {OperationName} 執行時間 {ElapsedMs}ms",
                operationName,
                elapsedMs);
        }
    }
}