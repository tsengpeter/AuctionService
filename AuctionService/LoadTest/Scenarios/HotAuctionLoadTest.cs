using System;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;

namespace AuctionService.LoadTest.Scenarios;

public class HotAuctionLoadTest
{
    private readonly string _baseUrl;
    private readonly MetricsCollector _metricsCollector;
    private readonly string _hotAuctionId;

    public HotAuctionLoadTest(string baseUrl, MetricsCollector metricsCollector, string hotAuctionId)
    {
        _baseUrl = baseUrl;
        _metricsCollector = metricsCollector;
        _hotAuctionId = !string.IsNullOrEmpty(hotAuctionId) 
            ? hotAuctionId 
            : "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    }

    public ScenarioProps CreateScenario()
    {
        var httpClient = Http.CreateDefaultClient();
        var random = new Random();

        return Scenario.Create("hot_auction_load_test", async context =>
        {
            // Randomly choose between getting auction details or current bid (70% details, 30% current bid)
            if (random.Next(100) < 70)
            {
                // Get hot auction details (70% of requests)
                var step1 = await Step.Run("get_hot_auction_detail", context, async () =>
                {
                    var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions/{_hotAuctionId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);

                    _metricsCollector.RecordResponse("HotAuctionLoadTest", response);

                    return response;
                });
            }
            else
            {
                // Get current bid for hot auction (30% of requests)
                var step2 = await Step.Run("get_hot_auction_current_bid", context, async () =>
                {
                    var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions/{_hotAuctionId}/current-bid")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);

                    _metricsCollector.RecordResponse("HotAuctionLoadTest", response);

                    return response;
                });
            }

            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
        );
    }
}