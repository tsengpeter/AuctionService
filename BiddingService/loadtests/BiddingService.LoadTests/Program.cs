using NBomber.CSharp;
using Microsoft.Extensions.Configuration;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

class Program
{
    static HttpClient CreateHttpClient(bool useHttps)
    {
        var handler = new HttpClientHandler();
        if (useHttps)
        {
            handler.ServerCertificateCustomValidationCallback = 
                (sender, cert, chain, sslPolicyErrors) => true;
        }
        // Configure connection settings for high concurrency
        handler.MaxConnectionsPerServer = 1000;
        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    static async Task WarmUpRedisAsync(string baseUrl, HttpClient client)
    {
        Console.WriteLine("\n🔥 Warming up Redis cache with test data...");
        
        var auctionIds = new[] { 123456789L, 987654321L, 111111111L };
        var bidderIds = Enumerable.Range(1, 50).Select(i => (long)i).ToArray();
        var random = new Random();
        var successCount = 0;
        
        // 為每個拍賣創建 50-100 筆競標
        foreach (var auctionId in auctionIds)
        {
            var bidCount = random.Next(50, 101);
            var baseAmount = 1000;
            
            for (int i = 0; i < bidCount; i++)
            {
                var bidderId = bidderIds[random.Next(bidderIds.Length)];
                var amount = baseAmount + (i * 100) + random.Next(1, 100);
                
                var bidRequest = new
                {
                    auctionId = auctionId,
                    amount = amount
                };
                
                var json = JsonSerializer.Serialize(bidRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // 使用測試模式的 X-Test-Bidder-Id header (需要 API 設定 BYPASS_AUTH=true)
                client.DefaultRequestHeaders.Remove("X-Test-Bidder-Id");
                client.DefaultRequestHeaders.Add("X-Test-Bidder-Id", bidderId.ToString());
                
                try
                {
                    var response = await client.PostAsync($"{baseUrl}/api/bids", content);
                    if (response.IsSuccessStatusCode)
                    {
                        successCount++;
                    }
                    else if (i < 3) // 只記錄前幾個失敗
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ Failed to create bid: {response.StatusCode} - {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    if (i < 3) // 只記錄前幾個異常
                    {
                        Console.WriteLine($"❌ Exception creating bid: {ex.Message}");
                    }
                }
                
                // 避免過快發送請求
                if (i % 10 == 0)
                {
                    await Task.Delay(100);
                }
            }
        }
        
        Console.WriteLine($"✅ Redis warm-up completed! Created {successCount} bids.");
        Console.WriteLine("⏳ Waiting 2 seconds for data to settle...\n");
        await Task.Delay(2000);
    }

    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var useHttps = bool.TryParse(config["UseHttps"], out var https) && https;
        var baseUrl = useHttps 
            ? (config["BaseUrlHttps"] ?? "https://localhost:7276")
            : (config["BaseUrl"] ?? "http://localhost:5107");
        
        // 設定台灣時區 (UTC+8)
        var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, taiwanTimeZone);
        
        Console.WriteLine($"🚀 Starting NBomber Load Tests for BiddingService");
        Console.WriteLine($"📍 Target: {baseUrl} ({(useHttps ? "HTTPS" : "HTTP")})");
        Console.WriteLine($"⏰ Taiwan Time: {taiwanTime:yyyy-MM-dd HH:mm:ss} (UTC+8)\n");

        // Create shared HttpClient instances
        var highestBidClient = CreateHttpClient(useHttps);
        var bidHistoryClient = CreateHttpClient(useHttps);
        var warmUpClient = CreateHttpClient(useHttps);

        // Warm up Redis before load testing
        await WarmUpRedisAsync(baseUrl, warmUpClient);

        // Scenario 1: Highest bid queries (5000 concurrent reads)
        var highestBidQueriesScenario = Scenario.Create("highest_bid_queries", async context =>
        {
            var auctionIds = new[] { 123456789L, 987654321L, 111111111L };
            var randomAuctionId = auctionIds[Random.Shared.Next(auctionIds.Length)];
            
            var response = await highestBidClient.GetAsync($"{baseUrl}/api/auctions/{randomAuctionId}/highest-bid");
            
            // For highest bid queries, both 200 (bid exists) and 404 (no bids) are valid responses
            return (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                ? Response.Ok(statusCode: ((int)response.StatusCode).ToString()) 
                : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Scenario 2: Bid history queries (1000 concurrent paginated reads)
        var bidHistoryQueriesScenario = Scenario.Create("bid_history_queries", async context =>
        {
            var auctionIds = new[] { 123456789L, 987654321L };
            var randomAuctionId = auctionIds[Random.Shared.Next(auctionIds.Length)];
            var page = Random.Shared.Next(1, 5);
            
            var response = await bidHistoryClient.GetAsync($"{baseUrl}/api/auctions/{randomAuctionId}/bids?page={page}&pageSize=20");
            
            return response.IsSuccessStatusCode 
                ? Response.Ok(statusCode: ((int)response.StatusCode).ToString()) 
                : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Run all scenarios
        var sessionName = $"tw_{taiwanTime:yyyyMMdd_HHmmss}";
        
        NBomberRunner
            .RegisterScenarios(
                highestBidQueriesScenario,
                bidHistoryQueriesScenario
            )
            .WithReportFileName(sessionName)
            .WithReportFolder("./reports")
            .Run();
        
        Console.WriteLine($"\n✅ Load test completed at {TimeZoneInfo.ConvertTime(DateTime.UtcNow, taiwanTimeZone):yyyy-MM-dd HH:mm:ss} (Taiwan Time)!");
    }
}
