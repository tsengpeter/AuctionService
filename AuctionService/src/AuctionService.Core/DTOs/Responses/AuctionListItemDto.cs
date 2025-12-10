using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Entities;

namespace AuctionService.Core.DTOs.Responses;

/// <summary>
/// 拍賣商品清單項目 DTO
/// </summary>
public class AuctionListItemDto
{
    /// <summary>
    /// 商品 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 商品名稱
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 商品描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 商品圖片 URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 起標價
    /// </summary>
    public decimal StartingPrice { get; set; }

    /// <summary>
    /// 目前價格
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// 拍賣開始時間
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 拍賣結束時間
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 拍賣狀態
    /// </summary>
    public AuctionStatus Status { get; set; }

    /// <summary>
    /// 商品類別
    /// </summary>
    public CategoryDto? Category { get; set; }

    /// <summary>
    /// 賣家資訊
    /// </summary>
    public SellerDto? Seller { get; set; }
}

/// <summary>
/// 商品類別 DTO
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// 類別 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 類別名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 賣家資訊 DTO
/// </summary>
public class SellerDto
{
    /// <summary>
    /// 賣家 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 賣家名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;
}