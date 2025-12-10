using AuctionService.Core.Entities;

namespace AuctionService.Core.Interfaces;

/// <summary>
/// 通用 Repository 介面
/// </summary>
/// <typeparam name="T">實體類型</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// 取得所有實體
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 根據 ID 取得實體
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// 新增實體
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// 更新實體
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// 刪除實體
    /// </summary>
    Task DeleteAsync(T entity);

    /// <summary>
    /// 檢查實體是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}