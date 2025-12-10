using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Api.Controllers;

/// <summary>
/// 拍賣商品控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuctionsController : BaseApiController
{
    private readonly IAuctionService _auctionService;
    private readonly ICategoryService _categoryService;
    private readonly IValidator<AuctionQueryParameters> _queryValidator;
    private readonly IValidator<CreateAuctionRequest> _createValidator;
    private readonly IValidator<UpdateAuctionRequest> _updateValidator;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="auctionService">拍賣商品服務</param>
    /// <param name="categoryService">商品分類服務</param>
    /// <param name="queryValidator">查詢參數驗證器</param>
    /// <param name="createValidator">建立拍賣請求驗證器</param>
    /// <param name="updateValidator">更新拍賣請求驗證器</param>
    public AuctionsController(
        IAuctionService auctionService,
        ICategoryService categoryService,
        IValidator<AuctionQueryParameters> queryValidator,
        IValidator<CreateAuctionRequest> createValidator,
        IValidator<UpdateAuctionRequest> updateValidator)
    {
        _auctionService = auctionService;
        _categoryService = categoryService;
        _queryValidator = queryValidator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 取得拍賣商品清單
    /// </summary>
    /// <param name="searchTerm">搜尋關鍵字</param>
    /// <param name="categoryId">分類 ID</param>
    /// <param name="status">拍賣狀態</param>
    /// <param name="minPrice">最低價格</param>
    /// <param name="maxPrice">最高價格</param>
    /// <param name="sortBy">排序欄位</param>
    /// <param name="sortDirection">排序方向</param>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <returns>分頁商品清單</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAuctions(
        [FromQuery] string? searchTerm,
        [FromQuery] int? categoryId,
        [FromQuery] AuctionStatus? status,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] AuctionSortBy? sortBy = AuctionSortBy.EndTime,
        [FromQuery] SortDirection? sortDirection = SortDirection.Ascending,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var parameters = new AuctionQueryParameters
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            Status = status,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortBy = sortBy ?? AuctionSortBy.EndTime,
            SortDirection = sortDirection ?? SortDirection.Ascending,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // 驗證查詢參數
        var validationResult = await _queryValidator.ValidateAsync(parameters);
        if (!validationResult.IsValid)
        {
            return Error("VALIDATION_ERROR", validationResult.Errors.First().ErrorMessage);
        }

        var result = await _auctionService.GetAuctionsAsync(parameters);
        return Success(result);
    }

    /// <summary>
    /// 根據 ID 取得商品詳細資訊
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <returns>商品詳細資訊</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuctionById(Guid id)
    {
        var auction = await _auctionService.GetAuctionByIdAsync(id);
        if (auction == null)
        {
            return Error("AUCTION_NOT_FOUND", "找不到商品");
        }

        return Success(auction);
    }

    /// <summary>
    /// 取得商品目前出價資訊
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <returns>目前出價資訊</returns>
    [HttpGet("{id}/current-bid")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentBid(Guid id)
    {
        var currentBid = await _auctionService.GetCurrentBidAsync(id);
        if (currentBid == null)
        {
            return Error("AUCTION_NOT_FOUND", "找不到商品");
        }

        return Success(currentBid);
    }

    /// <summary>
    /// 建立新的拍賣商品
    /// </summary>
    /// <param name="request">建立拍賣請求</param>
    /// <returns>建立的商品詳細資訊</returns>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionRequest request)
    {
        // 驗證請求
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Error("VALIDATION_ERROR", validationResult.Errors.First().ErrorMessage);
        }

        // 取得目前使用者 ID
        var userId = GetCurrentUserId();

        // 建立拍賣商品
        var auction = await _auctionService.CreateAuctionAsync(request, userId);

        return CreatedAtAction(
            nameof(GetAuctionById),
            new { id = auction.Id },
            Success(auction, "商品建立成功"));
    }

    /// <summary>
    /// 更新拍賣商品
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <param name="request">更新請求</param>
    /// <returns>更新後的商品詳細資訊</returns>
    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAuction(Guid id, [FromBody] UpdateAuctionRequest request)
    {
        // 驗證請求
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Error("VALIDATION_ERROR", validationResult.Errors.First().ErrorMessage);
        }

        // 取得目前使用者 ID
        var userId = GetCurrentUserId();

        // 更新拍賣商品
        var auction = await _auctionService.UpdateAuctionAsync(id, request, userId);

        return Success(auction, "商品更新成功");
    }

    /// <summary>
    /// 刪除拍賣商品
    /// </summary>
    /// <param name="id">商品 ID</param>
    /// <returns>刪除結果</returns>
    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAuction(Guid id)
    {
        // 取得目前使用者 ID
        var userId = GetCurrentUserId();

        // 刪除拍賣商品
        await _auctionService.DeleteAuctionAsync(id, userId);

        return Success(null, "商品刪除成功");
    }

    /// <summary>
    /// 取得使用者的拍賣商品
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="searchTerm">搜尋關鍵字</param>
    /// <param name="categoryId">分類 ID</param>
    /// <param name="status">拍賣狀態</param>
    /// <param name="minPrice">最低價格</param>
    /// <param name="maxPrice">最高價格</param>
    /// <param name="sortBy">排序欄位</param>
    /// <param name="sortDirection">排序方向</param>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <returns>分頁商品清單</returns>
    [Authorize]
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserAuctions(
        string userId,
        [FromQuery] string? searchTerm,
        [FromQuery] int? categoryId,
        [FromQuery] AuctionStatus? status,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] AuctionSortBy? sortBy = AuctionSortBy.EndTime,
        [FromQuery] SortDirection? sortDirection = SortDirection.Ascending,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var parameters = new AuctionQueryParameters
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            Status = status,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortBy = sortBy ?? AuctionSortBy.EndTime,
            SortDirection = sortDirection ?? SortDirection.Ascending,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // 驗證查詢參數
        var validationResult = await _queryValidator.ValidateAsync(parameters);
        if (!validationResult.IsValid)
        {
            return Error("VALIDATION_ERROR", validationResult.Errors.First().ErrorMessage);
        }

        var result = await _auctionService.GetUserAuctionsAsync(userId, parameters);
        return Success(result);
    }

    /// <summary>
    /// 取得所有商品分類
    /// </summary>
    /// <returns>商品分類清單</returns>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Success(categories);
    }
}