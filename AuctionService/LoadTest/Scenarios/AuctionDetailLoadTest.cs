using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;

namespace AuctionService.LoadTest.Scenarios;

public class AuctionDetailLoadTest
{
    private readonly string _baseUrl;
    private readonly MetricsCollector _metricsCollector;
    private readonly List<string> _auctionIds;

    public AuctionDetailLoadTest(string baseUrl, MetricsCollector metricsCollector, List<string> auctionIds)
    {
        _baseUrl = baseUrl;
        _metricsCollector = metricsCollector;
        _auctionIds = auctionIds ?? new List<string>();
    }

    public ScenarioProps CreateScenario()
    {
        var httpClient = Http.CreateDefaultClient();
        var random = new Random();

        return Scenario.Create("auction_detail_load_test", async context =>
        {
            var step1 = await Step.Run("get_auction_detail", context, async () =>
            {
                // Select a random auction ID for testing from pre-defined list
                var auctionId = _auctionIds.Count > 0
                    ? _auctionIds[(int)(context.InvocationNumber % _auctionIds.Count)]
                    : "11111111-1111-1111-1111-111111111111"; // Fallback to first test auction

                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions/{auctionId}")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("AuctionDetailLoadTest", response);

                return response;
            });

            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 150, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
        );
    }
}