using System;
using System.Collections.Generic;
using System.Linq;
using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using AuctionService.LoadTest.Scenarios;

namespace AuctionService.LoadTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════╗");
        Console.WriteLine("║   AuctionService Load Test Suite             ║");
        Console.WriteLine("╚═══════════════════════════════════════════════╝");
        Console.WriteLine();

        // Configuration
        var baseUrl = Environment.GetEnvironmentVariable("AUCTION_SERVICE_URL") ?? "http://localhost:5106";
        var metricsCollector = new MetricsCollector();

        Console.WriteLine($"📍 Target API: {baseUrl}");
        Console.WriteLine($"⏱️  Test Duration: Each scenario runs for 60 seconds");
        Console.WriteLine();
        Console.WriteLine("⚠️  請確保 AuctionService API 已經在運行中");
        Console.WriteLine();

        // Sample data for tests (使用固定的測試數據ID)
        var auctionIds = new List<string> { 
            "11111111-1111-1111-1111-111111111111", 
            "22222222-2222-2222-2222-222222222222", 
            "33333333-3333-3333-3333-333333333333" 
        };
        var userTokens = new List<string> { "mock-jwt-token" };
        var userIds = new List<string> { "test-user-1", "test-user-2" };
        var hotAuctionId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        // 建立測試場景
        var scenarios = new List<ScenarioProps>();

        // 1. Auction List - 拍賣列表查詢 (基礎負載)
        scenarios.Add(new AuctionListLoadTest(baseUrl, metricsCollector).CreateScenario());

        // 2. Auction Detail - 拍賣詳情查詢 (高頻讀取)
        scenarios.Add(new AuctionDetailLoadTest(baseUrl, metricsCollector, auctionIds).CreateScenario());

        // 3. Current Bid - 當前出價查詢 (極高頻)
        scenarios.Add(new CurrentBidLoadTest(baseUrl, metricsCollector, hotAuctionId).CreateScenario());

        // 4. Hot Auction - 熱門拍賣壓力測試
        scenarios.Add(new HotAuctionLoadTest(baseUrl, metricsCollector, hotAuctionId).CreateScenario());

        // 可選場景（需要認證）
        // scenarios.Add(new FollowLoadTest(baseUrl, metricsCollector, userTokens, auctionIds).CreateScenario());
        // scenarios.Add(new UserAuctionsLoadTest(baseUrl, metricsCollector, userIds, userTokens).CreateScenario());

        // 執行壓力測試
        Console.WriteLine("🚀 開始壓力測試...");
        Console.WriteLine();
        
        metricsCollector.StartCollection();

        var result = NBomberRunner
            .RegisterScenarios(scenarios.ToArray())
            .WithReportFileName($"auction_load_test_{DateTime.Now:yyyyMMdd_HHmmss}")
            .WithReportFolder("./reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Txt, ReportFormat.Csv)
            .Run();

        metricsCollector.StopCollection();

        // Print summary from NBomber results
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════╗");
        Console.WriteLine("║   Performance Targets Validation             ║");
        Console.WriteLine("╚═══════════════════════════════════════════════╝");
        Console.WriteLine();

        PrintScenarioResults(result, "auction_list_load_test", "Auction List", p95Target: 200, rpsTarget: 100, successRateTarget: 99.5);
        PrintScenarioResults(result, "auction_detail_load_test", "Auction Detail", p95Target: 150, rpsTarget: 150, successRateTarget: 99.9);
        PrintScenarioResults(result, "current_bid_load_test", "Current Bid", p95Target: 100, rpsTarget: 200, successRateTarget: 99.9);
        PrintScenarioResults(result, "follow_load_test", "Follow Operations", p95Target: 300, rpsTarget: 50, successRateTarget: 99.0);
        PrintScenarioResults(result, "hot_auction_load_test", "Hot Auction", p95Target: 50, rpsTarget: 1000, successRateTarget: 99.9);
        PrintScenarioResults(result, "user_auctions_load_test", "User Auctions", p95Target: 250, rpsTarget: 25, successRateTarget: 99.5);

        Console.WriteLine();
        Console.WriteLine($"📄 詳細報告已生成在 ./reports/ 目錄");
        Console.WriteLine();
        Console.WriteLine("Load testing completed.");
    }

    static void PrintScenarioResults(NodeStats result, string scenarioName, string displayName, double p95Target, double rpsTarget, double successRateTarget)
    {
        var scnStats = result.ScenarioStats.FirstOrDefault(s => s.ScenarioName == scenarioName);
        if (scnStats == null)
        {
            Console.WriteLine($"⚠ {displayName}: Scenario not found");
            return;
        }

        var globalStep = scnStats.StepStats.FirstOrDefault(s => s.StepName == "global information");
        if (globalStep == null)
        {
            Console.WriteLine($"⚠ {displayName}: Stats not available");
            return;
        }

        var p95 = globalStep.Ok.Latency.Percent95;
        var rps = globalStep.Ok.Request.RPS;
        var successRate = (double)globalStep.Ok.Request.Count / (globalStep.Ok.Request.Count + globalStep.Fail.Request.Count) * 100;

        Console.WriteLine($"[{displayName}]");
        Console.WriteLine($"  P95 Latency: {p95:F2}ms {(p95 <= p95Target ? "✓" : "✗")} (target: ≤{p95Target}ms)");
        Console.WriteLine($"  RPS:         {rps:F2} {(rps >= rpsTarget ? "✓" : "✗")} (target: ≥{rpsTarget})");
        Console.WriteLine($"  Success:     {successRate:F2}% {(successRate >= successRateTarget ? "✓" : "✗")} (target: ≥{successRateTarget}%)");
        Console.WriteLine();
    }
}