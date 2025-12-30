using AuctionService.Core.Entities;

namespace AuctionService.Core.DTOs.Common;

/// <summary>
/// 拍賣商品查詢參數
/// </summary>
public class AuctionQueryParameters : PaginationParameters
{
    /// <summary>
    /// 搜尋關鍵字 (商品名稱或描述)
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// 商品類別 ID
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// 拍賣狀態篩選
    /// </summary>
    public AuctionStatus? Status { get; set; }

    /// <summary>
    /// 價格範圍 - 最低價
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// 價格範圍 - 最高價
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// 排序欄位
    /// </summary>
    public AuctionSortBy SortBy { get; set; } = AuctionSortBy.EndTime;

    /// <summary>
    /// 排序方向
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
}

/// <summary>
/// 拍賣狀態
/// </summary>
public enum AuctionStatus
{
    /// <summary>
    /// 待開始
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 進行中
    /// </summary>
    Active = 1,

    /// <summary>
    /// 已結束
    /// </summary>
    Ended = 2
}

/// <summary>
/// 排序欄位
/// </summary>
public enum AuctionSortBy
{
    /// <summary>
    /// 結束時間
    /// </summary>
    EndTime = 0,

    /// <summary>
    /// 開始時間
    /// </summary>
    StartTime = 1,

    /// <summary>
    /// 目前價格
    /// </summary>
    CurrentPrice = 2,

    /// <summary>
    /// 商品名稱
    /// </summary>
    Title = 3,

    /// <summary>
    /// 建立時間
    /// </summary>
    CreatedAt = 4
}

/// <summary>
/// 排序方向
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// 升冪
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// 降冪
    /// </summary>
    Descending = 1
}