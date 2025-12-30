using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Interfaces;
using System.Net.Http.Json;

namespace AuctionService.Infrastructure.Clients;

/// <summary>
/// 出價服務客戶端實作
/// </summary>
public class BiddingServiceClient : IBiddingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IResponseCodeService _responseCodeService;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="httpClient">HTTP 客戶端</param>
    /// <param name="responseCodeService">響應代碼服務</param>
    public BiddingServiceClient(HttpClient httpClient, IResponseCodeService responseCodeService)
    {
        _httpClient = httpClient;
        _responseCodeService = responseCodeService;
    }

    /// <summary>
    /// 取得商品的目前出價資訊
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>目前出價資訊</returns>
    public async Task<CurrentBidDto?> GetCurrentBidAsync(Guid auctionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/auctions/{auctionId}/current-bid");
            if (response.IsSuccessStatusCode)
            {
                var result = await HandleApiResponseAsync<CurrentBidDto>(response);
                return result?.Data;
            }
            return null;
        }
        catch
        {
            // 如果 BiddingService 不可用，返回 null
            // 這樣 AuctionService 仍然可以正常運作
            return null;
        }
    }

    /// <summary>
    /// 檢查商品是否已有出價
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <returns>是否已有出價</returns>
    public async Task<bool> CheckAuctionHasBidsAsync(Guid auctionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/auctions/{auctionId}/has-bids");
            if (response.IsSuccessStatusCode)
            {
                var result = await HandleApiResponseAsync<bool>(response);
                return result?.Data ?? false;
            }
            return false;
        }
        catch
        {
            // 如果 BiddingService 不可用，假設沒有出價
            // 這樣可以讓業務邏輯繼續運作
            return false;
        }
    }

    private async Task<ApiResponse<T>> HandleApiResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        if (apiResponse == null)
        {
            throw new InvalidOperationException($"Failed to deserialize API response for type {typeof(T).Name}");
        }

        if (!string.IsNullOrEmpty(apiResponse.Message))
        {
            apiResponse.LocalizedMessage = await _responseCodeService.GetLocalizedMessageAsync(apiResponse.Message);
        }
        return apiResponse;
    }
}

/// <summary>
/// API 回應包裝類別 (用於與 BiddingService 通訊)
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 訊息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 本地化的訊息
    /// </summary>
    public string? LocalizedMessage { get; set; }

    /// <summary>
    /// 資料
    /// </summary>
    public T? Data { get; set; }
}