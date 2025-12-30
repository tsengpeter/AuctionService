using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.Exceptions;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Api.Controllers;

/// <summary>
/// 商品追蹤控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowsController : BaseApiController
{
    private readonly IFollowService _followService;
    private readonly IValidator<AuctionQueryParameters> _queryValidator;
    private readonly IValidator<FollowAuctionRequest> _followValidator;

    /// <summary>
    /// 建構子
    /// </summary>
    public FollowsController(
        IFollowService followService,
        IValidator<AuctionQueryParameters> queryValidator,
        IValidator<FollowAuctionRequest> followValidator,
        IResponseCodeService responseCodeService)
        : base(responseCodeService)
    {
        _followService = followService;
        _queryValidator = queryValidator;
        _followValidator = followValidator;
    }

    /// <summary>
    /// 取得使用者追蹤清單
    /// </summary>
    /// <param name="sortBy">排序欄位</param>
    /// <param name="sortDirection">排序方向</param>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <returns>分頁追蹤清單</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFollows(
        [FromQuery] AuctionSortBy? sortBy = AuctionSortBy.CreatedAt,
        [FromQuery] SortDirection? sortDirection = SortDirection.Descending,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();

        var parameters = new AuctionQueryParameters
        {
            SortBy = sortBy ?? AuctionSortBy.CreatedAt,
            SortDirection = sortDirection ?? SortDirection.Descending,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // 驗證查詢參數
        var validationResult = await _queryValidator.ValidateAsync(parameters);
        if (!validationResult.IsValid)
        {
            return await Error("VALIDATION_ERROR", validationResult.Errors.First().ErrorMessage);
        }

        var result = await _followService.GetUserFollowsAsync(userId, parameters);
        return await Success(result);
    }

    /// <summary>
    /// 追蹤商品
    /// </summary>
    /// <param name="request">追蹤請求</param>
    /// <returns>追蹤記錄</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FollowAuction([FromBody] FollowAuctionRequest request)
    {
        var userId = GetCurrentUserId();

        // 驗證請求
        var validationResult = await _followValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return await Error("VALIDATION_ERROR", validationResult.Errors.First().ErrorMessage);
        }

        try
        {
            var followDto = await _followService.AddFollowAsync(userId, request.AuctionId);
            
            // 建立成功回應
            var language = GetRequestLanguage();
            var responseInfo = await _responseCodeService.GetLocalizedResponseAsync("SUCCESS", language);

            return CreatedAtAction(
                nameof(GetFollows),
                null,
                new
                {
                    success = true,
                    statusCode = responseInfo?.Code ?? "SUCCESS",
                    statusName = responseInfo?.Name ?? "Success",
                    message = responseInfo?.Message ?? "追蹤成功",
                    data = followDto
                });
        }
        catch (AuctionService.Core.Exceptions.ValidationException ex) when (ex.Message.Contains("Already following"))
        {
            return await Error("DUPLICATE_FOLLOW", "Already following this auction");
        }
        catch (AuctionService.Core.Exceptions.ValidationException ex) when (ex.Message.Contains("Cannot follow your own"))
        {
            return await Error("VALIDATION_ERROR", "Cannot follow your own auction");
        }
        catch (AuctionService.Core.Exceptions.ValidationException ex) when (ex.Message.Contains("Maximum follow limit"))
        {
            return await Error("VALIDATION_ERROR", "Maximum follow limit exceeded");
        }
        catch (AuctionNotFoundException)
        {
            return await Error("AUCTION_NOT_FOUND", "Auction not found");
        }
    }

    /// <summary>
    /// 取消追蹤商品
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <returns></returns>
    [HttpDelete("{auctionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfollowAuction(Guid auctionId)
    {
        var userId = GetCurrentUserId();

        try
        {
            await _followService.RemoveFollowAsync(userId, auctionId);
            return NoContent();
        }
        catch (AuctionService.Core.Exceptions.ValidationException)
        {
            return NotFound("Follow record not found");
        }
    }
}