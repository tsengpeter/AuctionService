using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Extensions;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Validators;
using FluentValidation;

namespace AuctionService.Core.Services;

/// <summary>
/// 拍賣商品服務實作
/// </summary>
public class AuctionService : IAuctionService
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBiddingServiceClient _biddingServiceClient;
    private readonly CreateAuctionRequestValidator _createValidator;

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
        _createValidator = new CreateAuctionRequestValidator();
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

    /// <summary>
    /// 建立新的拍賣商品
    /// </summary>
    /// <param name="request">建立請求</param>
    /// <param name="userId">賣家使用者 ID</param>
    /// <returns>建立的商品詳細資訊</returns>
    public async Task<AuctionDetailDto> CreateAuctionAsync(CreateAuctionRequest request, string userId)
    {
        // 驗證請求
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            StartingPrice = request.StartingPrice,
            CategoryId = request.CategoryId,
            StartTime = request.StartTime ?? DateTime.UtcNow,
            EndTime = request.EndTime,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdAuction = await _auctionRepository.CreateAsync(auction);
        return MapToAuctionDetailDto(createdAuction);
    }

    /// <summary>
    /// 更新拍賣商品
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <param name="request">更新請求</param>
    /// <param name="userId">賣家使用者 ID</param>
    /// <returns>更新後的商品詳細資訊</returns>
    public async Task<AuctionDetailDto> UpdateAuctionAsync(Guid id, UpdateAuctionRequest request, string userId)
    {
        var auction = await _auctionRepository.GetByIdAsync(id);
        if (auction == null || auction.UserId != userId)
        {
            throw new InvalidOperationException("Auction not found or access denied");
        }

        // 檢查是否已有出價，如果有則不允許更新
        var hasBids = await _biddingServiceClient.CheckAuctionHasBidsAsync(id);
        if (hasBids)
        {
            throw new InvalidOperationException("Cannot update auction that already has bids");
        }

        auction.Name = request.Name;
        auction.Description = request.Description;
        auction.StartingPrice = request.StartingPrice;
        auction.EndTime = request.EndTime;
        auction.UpdatedAt = DateTime.UtcNow;

        var updatedAuction = await _auctionRepository.UpdateAsync(auction);
        return MapToAuctionDetailDto(updatedAuction);
    }

    /// <summary>
    /// 刪除拍賣商品
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <param name="userId">賣家使用者 ID</param>
    /// <returns>刪除成功</returns>
    public async Task DeleteAuctionAsync(Guid id, string userId)
    {
        var auction = await _auctionRepository.GetByIdAsync(id);
        if (auction == null || auction.UserId != userId)
        {
            throw new InvalidOperationException("Auction not found or access denied");
        }

        // 檢查是否已有出價，如果有則不允許刪除
        var hasBids = await _biddingServiceClient.CheckAuctionHasBidsAsync(id);
        if (hasBids)
        {
            throw new InvalidOperationException("Cannot delete auction that already has bids");
        }

        await _auctionRepository.DeleteAsync(id);
    }

    /// <summary>
    /// 取得使用者的拍賣商品
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="parameters">分頁參數</param>
    /// <returns>分頁結果</returns>
    public async Task<PagedResult<AuctionListItemDto>> GetUserAuctionsAsync(string userId, AuctionQueryParameters parameters)
    {
        var (auctions, totalCount) = await _auctionRepository.GetByUserIdAsync(userId, parameters);
        var auctionDtos = auctions.Select(MapToAuctionListItemDto).ToList();

        return new PagedResult<AuctionListItemDto>
        {
            Items = auctionDtos,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}