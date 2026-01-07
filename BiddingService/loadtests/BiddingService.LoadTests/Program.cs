using NBomber.CSharp;
using Microsoft.Extensions.Configuration;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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

    static void Main(string[] args)
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
