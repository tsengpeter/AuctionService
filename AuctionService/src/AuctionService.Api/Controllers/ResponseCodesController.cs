using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Api.Controllers;

/// <summary>
/// 回應代碼控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ResponseCodesController : BaseApiController
{
    private readonly IResponseCodeService _responseCodeService;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="responseCodeService">回應代碼服務</param>
    public ResponseCodesController(IResponseCodeService responseCodeService)
    {
        _responseCodeService = responseCodeService;
    }

    /// <summary>
    /// 取得所有回應代碼
    /// </summary>
    /// <returns>回應代碼清單</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResponseCodeDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var responseCodes = await _responseCodeService.GetAllAsync();
        return Ok(responseCodes);
    }

    /// <summary>
    /// 根據代碼取得回應代碼
    /// </summary>
    /// <param name="code">代碼</param>
    /// <returns>回應代碼</returns>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ResponseCodeDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByCode(string code)
    {
        var responseCode = await _responseCodeService.GetByCodeAsync(code);
        if (responseCode == null)
        {
            return NotFound();
        }
        return Ok(responseCode);
    }

    /// <summary>
    /// 根據分類取得回應代碼
    /// </summary>
    /// <param name="category">分類</param>
    /// <returns>回應代碼清單</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<ResponseCodeDto>), 200)]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var responseCodes = await _responseCodeService.GetByCategoryAsync(category);
        return Ok(responseCodes);
    }

    /// <summary>
    /// 取得本地化訊息
    /// </summary>
    /// <param name="code">代碼</param>
    /// <param name="language">語言 (zh-tw 或 en)</param>
    /// <returns>本地化訊息</returns>
    [HttpGet("{code}/localized")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetLocalizedMessage(string code, [FromQuery] string language = "zh-tw")
    {
        var message = await _responseCodeService.GetLocalizedMessageAsync(code, language);
        if (message == null)
        {
            return NotFound();
        }
        return Ok(message);
    }
}