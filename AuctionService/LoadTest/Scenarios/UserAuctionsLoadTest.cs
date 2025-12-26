using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;

namespace AuctionService.LoadTest.Scenarios;

public class UserAuctionsLoadTest
{
    private readonly string _baseUrl;
    private readonly MetricsCollector _metricsCollector;
    private readonly List<string> _userIds;
    private readonly List<string> _userTokens;

    public UserAuctionsLoadTest(string baseUrl, MetricsCollector metricsCollector, List<string> userIds, List<string> userTokens)
    {
        _baseUrl = baseUrl;
        _metricsCollector = metricsCollector;
        _userIds = userIds ?? new List<string>();
        _userTokens = userTokens ?? new List<string>();
    }

    public ScenarioProps CreateScenario()
    {
        var httpClient = Http.CreateDefaultClient();
        var random = new Random();

        return Scenario.Create("user_auctions_load_test", async context =>
        {
            var step = await Step.Run("get_user_auctions", context, async () =>
            {
                var userId = _userIds.Count > 0 ? _userIds[random.Next(_userIds.Count)] : Guid.NewGuid().ToString();
                var userToken = _userTokens.Count > 0 ? _userTokens[random.Next(_userTokens.Count)] : "mock-jwt-token";

                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/auctions/user/{userId}?pageSize=20")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Authorization", $"Bearer {userToken}");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("UserAuctionsLoadTest", response);

                return response;
            });

            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 25, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
        );
    }
}