using BiddingService.Core.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BiddingService.Infrastructure.HttpClients;

public class AuctionServiceClient : IAuctionServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuctionServiceClient> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuctionServiceClient(HttpClient httpClient, IMemoryCache cache, ILogger<AuctionServiceClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AuctionInfo?> GetAuctionAsync(long auctionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/auctions/{auctionId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AuctionInfo>(content, _jsonOptions);
            }
            
            _logger.LogWarning(
                "Failed to get auction {AuctionId} from AuctionService. Status: {StatusCode}",
                auctionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Exception occurred while getting auction {AuctionId} from AuctionService",
                auctionId);
            return null;
        }
    }

    public async Task<bool> ValidateAuctionAsync(long auctionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/auctions/{auctionId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to validate auction {AuctionId} from AuctionService. Status: {StatusCode}",
                    auctionId, response.StatusCode);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while validating auction {AuctionId} from AuctionService",
                auctionId);
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
            bool batchSuccess = false;
            try
            {
                var requestContent = JsonSerializer.Serialize(new { auctionIds = missingAuctionIds });
                var response = await _httpClient.PostAsync("api/auctions/batch", new StringContent(requestContent, System.Text.Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var fetchedAuctions = JsonSerializer.Deserialize<IEnumerable<AuctionInfo>>(content, _jsonOptions) ?? Enumerable.Empty<AuctionInfo>();

                    // Cache the fetched auctions
                    foreach (var auction in fetchedAuctions)
                    {
                        _cache.Set($"auction:{auction.Id}", auction, TimeSpan.FromMinutes(5));
                        auctionInfos.Add(auction);
                    }
                    batchSuccess = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Batch request for auctions failed, falling back to individual requests. AuctionIds: {AuctionIds}",
                    string.Join(", ", missingAuctionIds));
            }

            // If batch fetch fails, try individual fetches for missing auctions
            if (!batchSuccess)
            {
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
            
            _logger.LogWarning(
                "Failed to get starting price for auction {AuctionId} from AuctionService. Status: {StatusCode}",
                auctionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while getting starting price for auction {AuctionId} from AuctionService",
                auctionId);
            return null;
        }
    }
}
