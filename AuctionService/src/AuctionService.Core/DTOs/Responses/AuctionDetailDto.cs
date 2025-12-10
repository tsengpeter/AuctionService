using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Entities;

namespace AuctionService.Core.DTOs.Responses;

/// <summary>
/// 拍賣商品詳細資訊 DTO
/// </summary>
public class AuctionDetailDto
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
    /// 商品圖片 URL 清單
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();

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

    /// <summary>
    /// 出價記錄
    /// </summary>
    public List<BidDto> Bids { get; set; } = new();
}

/// <summary>
/// 出價記錄 DTO
/// </summary>
public class BidDto
{
    /// <summary>
    /// 出價 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 出價金額
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 出價時間
    /// </summary>
    public DateTime BidTime { get; set; }

    /// <summary>
    /// 出價者資訊
    /// </summary>
    public BidderDto? Bidder { get; set; }
}

/// <summary>
/// 出價者資訊 DTO
/// </summary>
public class BidderDto
{
    /// <summary>
    /// 出價者 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 出價者名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;
}