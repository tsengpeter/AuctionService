using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Exceptions;
using AuctionService.Core.Extensions;
using AuctionService.Core.Interfaces;

namespace AuctionService.Core.Services;

/// <summary>
/// 商品追蹤服務實作
/// </summary>
public class FollowService : IFollowService
{
    private readonly IFollowRepository _followRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IBiddingServiceClient _biddingServiceClient;

    /// <summary>
    /// 建構子
    /// </summary>
    public FollowService(
        IFollowRepository followRepository,
        IAuctionRepository auctionRepository,
        IBiddingServiceClient biddingServiceClient)
    {
        _followRepository = followRepository;
        _auctionRepository = auctionRepository;
        _biddingServiceClient = biddingServiceClient;
    }

    /// <summary>
    /// 新增追蹤
    /// </summary>
    public async Task<FollowDto> AddFollowAsync(string userId, Guid auctionId)
    {
        // 檢查商品是否存在
        var auction = await _auctionRepository.GetAuctionByIdAsync(auctionId);
        if (auction == null)
        {
            throw new AuctionNotFoundException(auctionId);
        }

        // 檢查是否已經追蹤
        if (await _followRepository.ExistsAsync(userId, auctionId))
        {
            throw new ValidationException("Already following this auction");
        }

        // 檢查是否追蹤自己的商品
        if (await _followRepository.IsFollowingOwnAuctionAsync(userId, auctionId))
        {
            throw new ValidationException("Cannot follow your own auction");
        }

        // 檢查追蹤數量限制 (最多 500 個)
        var (follows, _) = await _followRepository.GetByUserIdAsync(userId, new AuctionQueryParameters { PageNumber = 1, PageSize = 1000 });
        if (follows.Count() >= 500)
        {
            throw new ValidationException("Maximum follow limit (500) exceeded");
        }

        // 建立追蹤記錄
        var follow = new Follow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AuctionId = auctionId,
            CreatedAt = DateTime.UtcNow
        };

        var savedFollow = await _followRepository.AddAsync(follow);

        // 取得目前出價資訊
        var currentBid = await _biddingServiceClient.GetCurrentBidAsync(auctionId);

        // 轉換為 DTO
        return new FollowDto
        {
            FollowId = savedFollow.Id,
            UserId = savedFollow.UserId,
            AuctionId = savedFollow.AuctionId,
            AuctionName = auction.Name,
            AuctionStatus = auction.CalculateStatus(),
            CurrentBid = currentBid?.CurrentPrice,
            EndTime = auction.EndTime,
            CreatedAt = savedFollow.CreatedAt
        };
    }

    /// <summary>
    /// 移除追蹤
    /// </summary>
    public async Task RemoveFollowAsync(string userId, Guid auctionId)
    {
        // 檢查追蹤是否存在
        if (!await _followRepository.ExistsAsync(userId, auctionId))
        {
            throw new ValidationException("Follow record not found");
        }

        await _followRepository.RemoveAsync(userId, auctionId);
    }

    /// <summary>
    /// 取得使用者追蹤清單
    /// </summary>
    public async Task<PagedResult<FollowDto>> GetUserFollowsAsync(string userId, AuctionQueryParameters parameters)
    {
        var (follows, totalCount) = await _followRepository.GetByUserIdAsync(userId, parameters);

        var followDtos = new List<FollowDto>();

        foreach (var follow in follows)
        {
            if (follow.Auction == null) continue;

            // 取得目前出價資訊
            var currentBid = await _biddingServiceClient.GetCurrentBidAsync(follow.AuctionId);

            followDtos.Add(new FollowDto
            {
                FollowId = follow.Id,
                UserId = follow.UserId,
                AuctionId = follow.AuctionId,
                AuctionName = follow.Auction.Name,
                AuctionStatus = follow.Auction.CalculateStatus(),
                CurrentBid = currentBid?.CurrentPrice,
                EndTime = follow.Auction.EndTime,
                CreatedAt = follow.CreatedAt
            });
        }

        return new PagedResult<FollowDto>
        {
            Items = followDtos,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 檢查追蹤是否存在
    /// </summary>
    public async Task<bool> CheckFollowExistsAsync(string userId, Guid auctionId)
    {
        return await _followRepository.ExistsAsync(userId, auctionId);
    }
}