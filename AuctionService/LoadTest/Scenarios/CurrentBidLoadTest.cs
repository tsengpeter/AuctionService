using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;

namespace AuctionService.LoadTest.Scenarios;

public class CurrentBidLoadTest
{
    private readonly string _baseUrl;
    private readonly MetricsCollector _metricsCollector;
    private readonly string _hotAuctionId;

    public CurrentBidLoadTest(string baseUrl, MetricsCollector metricsCollector, string hotAuctionId)
    {
        _baseUrl = baseUrl;
        _metricsCollector = metricsCollector;
        _hotAuctionId = hotAuctionId ?? Guid.NewGuid().ToString();
    }

    public ScenarioProps CreateScenario()
    {
        var httpClient = Http.CreateDefaultClient();

        return Scenario.Create("current_bid_load_test", async context =>
        {
            var step = await Step.Run("get_current_bid", context, async () =>
            {
                // Always use the hot auction ID for consistency
                var auctionId = !string.IsNullOrEmpty(_hotAuctionId) 
                    ? _hotAuctionId 
                    : "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
                    
                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions/{auctionId}/current-bid")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("CurrentBidLoadTest", response);

                return response;
            });

            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(120))
        );
    }
}