using BiddingService.Core.Interfaces;
using System.Text.Json;

namespace BiddingService.Infrastructure.HttpClients;

public class AuctionServiceClient : IAuctionServiceClient
{
    private readonly HttpClient _httpClient;

    public AuctionServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuctionInfo?> GetAuctionAsync(long auctionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/auctions/{auctionId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AuctionInfo>(content);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ValidateAuctionAsync(long auctionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/auctions/{auctionId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<decimal?> GetStartingPriceAsync(long auctionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/auctions/{auctionId}/starting-price");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return decimal.Parse(content);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}