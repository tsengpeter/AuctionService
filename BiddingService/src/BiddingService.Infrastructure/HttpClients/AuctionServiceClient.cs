using BiddingService.Core.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace BiddingService.Infrastructure.HttpClients;

public class AuctionServiceClient : IAuctionServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public AuctionServiceClient(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
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

    public async Task<IEnumerable<AuctionInfo>> GetAuctionsBatchAsync(IEnumerable<long> auctionIds)
    {
        var auctionInfos = new List<AuctionInfo>();
        var missingAuctionIds = new List<long>();

        // Check cache first
        foreach (var auctionId in auctionIds)
        {
            if (_cache.TryGetValue($"auction:{auctionId}", out AuctionInfo? cachedAuction) && cachedAuction != null)
            {
                auctionInfos.Add(cachedAuction);
            }
            else
            {
                missingAuctionIds.Add(auctionId);
            }
        }

        // Fetch missing auctions from service
        if (missingAuctionIds.Any())
        {
            try
            {
                var requestContent = JsonSerializer.Serialize(new { auctionIds = missingAuctionIds });
                var response = await _httpClient.PostAsync("api/auctions/batch", new StringContent(requestContent, System.Text.Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var fetchedAuctions = JsonSerializer.Deserialize<IEnumerable<AuctionInfo>>(content) ?? Enumerable.Empty<AuctionInfo>();

                    // Cache the fetched auctions
                    foreach (var auction in fetchedAuctions)
                    {
                        _cache.Set($"auction:{auction.Id}", auction, TimeSpan.FromMinutes(5));
                        auctionInfos.Add(auction);
                    }
                }
            }
            catch
            {
                // If batch fetch fails, try individual fetches for missing auctions
                foreach (var auctionId in missingAuctionIds)
                {
                    var auction = await GetAuctionAsync(auctionId);
                    if (auction != null)
                    {
                        _cache.Set($"auction:{auctionId}", auction, TimeSpan.FromMinutes(5));
                        auctionInfos.Add(auction);
                    }
                }
            }
        }

        return auctionInfos;
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
