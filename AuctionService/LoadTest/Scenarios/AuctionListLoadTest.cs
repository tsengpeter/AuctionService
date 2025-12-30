using System;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;

namespace AuctionService.LoadTest.Scenarios;

public class AuctionListLoadTest
{
    private readonly string _baseUrl;
    private readonly MetricsCollector _metricsCollector;

    public AuctionListLoadTest(string baseUrl, MetricsCollector metricsCollector)
    {
        _baseUrl = baseUrl;
        _metricsCollector = metricsCollector;
    }

    public ScenarioProps CreateScenario()
    {
        var httpClient = Http.CreateDefaultClient();

        return Scenario.Create("auction_list_load_test", async context =>
        {
            // Basic auction list
            var step1 = await Step.Run("get_auctions_basic", context, async () =>
            {
                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions?pageSize=20")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("AuctionListLoadTest", response);

                return response;
            });

            // Auction list with category filter
            var step2 = await Step.Run("get_auctions_by_category", context, async () =>
            {
                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions?categoryId=1&pageSize=20")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("AuctionListLoadTest", response);

                return response;
            });

            // Auction list with search
            var step3 = await Step.Run("get_auctions_search", context, async () =>
            {
                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions?search=iPhone&pageSize=20")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("AuctionListLoadTest", response);

                return response;
            });

            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
        );
    }
}