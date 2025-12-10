using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Extensions;
using AuctionService.Core.Interfaces;

namespace AuctionService.Core.Services;

/// <summary>
/// 拍賣商品服務實作
/// </summary>
public class AuctionService : IAuctionService
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBiddingServiceClient _biddingServiceClient;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="auctionRepository">拍賣商品資料庫存取</param>
    /// <param name="categoryRepository">商品分類資料庫存取</param>
    /// <param name="biddingServiceClient">出價服務客戶端</param>
    public AuctionService(
        IAuctionRepository auctionRepository,
        ICategoryRepository categoryRepository,
        IBiddingServiceClient biddingServiceClient)
    {
        _auctionRepository = auctionRepository;
        _categoryRepository = categoryRepository;
        _biddingServiceClient = biddingServiceClient;
    }

    /// <summary>
    /// 取得拍賣商品清單
    /// </summary>
    /// <param name="parameters">查詢參數</param>
    /// <returns>分頁結果</returns>
    public async Task<PagedResult<AuctionListItemDto>> GetAuctionsAsync(AuctionQueryParameters parameters)
    {
        // 取得拍賣商品清單
        var (auctions, totalCount) = await _auctionRepository.GetAuctionsAsync(parameters);

        // 轉換為 DTO
        var auctionDtos = auctions.Select(MapToAuctionListItemDto).ToList();

        return new PagedResult<AuctionListItemDto>
        {
            Items = auctionDtos,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 根據 ID 取得拍賣商品詳細資訊
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <returns>商品詳細資訊</returns>
    public async Task<AuctionDetailDto?> GetAuctionByIdAsync(Guid id)
    {
        var auction = await _auctionRepository.GetAuctionByIdAsync(id);
        if (auction == null)
        {
            return null;
        }

        return MapToAuctionDetailDto(auction);
    }

    /// <summary>
    /// 取得商品目前出價資訊
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>目前出價資訊</returns>
    public async Task<CurrentBidDto?> GetCurrentBidAsync(Guid auctionId)
    {
        // 首先檢查商品是否存在
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        if (auction == null)
        {
            return null;
        }

        // 從 BiddingService 取得目前出價資訊
        var currentBid = await _biddingServiceClient.GetCurrentBidAsync(auctionId);

        // 如果 BiddingService 不可用，返回基本資訊
        return currentBid ?? new CurrentBidDto
        {
            AuctionId = auctionId,
            CurrentPrice = auction.StartingPrice,
            BidCount = 0,
            LastBidTime = null,
            HighestBidder = null
        };
    }

    /// <summary>
    /// 將 Auction 實體對應到 AuctionListItemDto
    /// </summary>
    private static AuctionListItemDto MapToAuctionListItemDto(Auction auction)
    {
        return new AuctionListItemDto
        {
            Id = auction.Id,
            Title = auction.Name,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.StartingPrice, // 目前先用起標價，之後需要實作出價邏輯
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            Status = auction.CalculateStatus(),
            Category = auction.Category != null ? new CategoryDto
            {
                Id = auction.Category.Id,
                Name = auction.Category.Name
            } : null,
            Seller = new SellerDto
            {
                Id = auction.UserId,
                Username = $"User_{auction.UserId}" // 暫時的實作
            }
        };
    }

    /// <summary>
    /// 將 Auction 實體對應到 AuctionDetailDto
    /// </summary>
    private static AuctionDetailDto MapToAuctionDetailDto(Auction auction)
    {
        return new AuctionDetailDto
        {
            Id = auction.Id,
            Title = auction.Name,
            Description = auction.Description,
            ImageUrls = new List<string>(), // 目前沒有圖片功能
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.StartingPrice, // 目前先用起標價
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            Status = auction.CalculateStatus(),
            Category = auction.Category != null ? new CategoryDto
            {
                Id = auction.Category.Id,
                Name = auction.Category.Name
            } : null,
            Seller = new SellerDto
            {
                Id = auction.UserId,
                Username = $"User_{auction.UserId}" // 暫時的實作
            },
            Bids = new List<BidDto>() // 目前沒有出價記錄
        };
    }
}