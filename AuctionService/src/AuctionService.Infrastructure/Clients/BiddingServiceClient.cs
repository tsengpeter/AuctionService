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

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="httpClient">HTTP 客戶端</param>
    public BiddingServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentBidDto>>();
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
    /// 資料
    /// </summary>
    public T? Data { get; set; }
}