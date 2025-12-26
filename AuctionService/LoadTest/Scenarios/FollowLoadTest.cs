using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;

namespace AuctionService.LoadTest.Scenarios;

public class FollowLoadTest
{
    private readonly string _baseUrl;
    private readonly MetricsCollector _metricsCollector;
    private readonly List<string> _userTokens;
    private readonly List<string> _auctionIds;

    public FollowLoadTest(string baseUrl, MetricsCollector metricsCollector, List<string> userTokens, List<string> auctionIds)
    {
        _baseUrl = baseUrl;
        _metricsCollector = metricsCollector;
        _userTokens = userTokens ?? new List<string>();
        _auctionIds = auctionIds ?? new List<string>();
    }

    public ScenarioProps CreateScenario()
    {
        var httpClient = Http.CreateDefaultClient();
        var random = new Random();

        return Scenario.Create("follow_load_test", async context =>
        {
            // Create follow
            var step1 = await Step.Run("create_follow", context, async () =>
            {
                var userToken = _userTokens.Count > 0 ? _userTokens[random.Next(_userTokens.Count)] : "mock-jwt-token";
                var auctionId = _auctionIds.Count > 0 ? _auctionIds[random.Next(_auctionIds.Count)] : Guid.NewGuid().ToString();

                var request = Http.CreateRequest("POST", $"{_baseUrl}/api/follows")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Authorization", $"Bearer {userToken}")
                    .WithJsonBody(new { AuctionId = auctionId });

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("FollowLoadTest", response);

                return response;
            });

            // Get user follows
            var step2 = await Step.Run("get_user_follows", context, async () =>
            {
                var userToken = _userTokens.Count > 0 ? _userTokens[random.Next(_userTokens.Count)] : "mock-jwt-token";

                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/follows?pageSize=20")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Authorization", $"Bearer {userToken}");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("FollowLoadTest", response);

                return response;
            });

            // Delete follow
            var step3 = await Step.Run("delete_follow", context, async () =>
            {
                var userToken = _userTokens.Count > 0 ? _userTokens[random.Next(_userTokens.Count)] : "mock-jwt-token";
                var auctionId = _auctionIds.Count > 0 ? _auctionIds[random.Next(_auctionIds.Count)] : Guid.NewGuid().ToString();

                var request = Http.CreateRequest("DELETE", $"{_baseUrl}/api/follows/{auctionId}")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Authorization", $"Bearer {userToken}");

                var response = await Http.Send(httpClient, request);

                _metricsCollector.RecordResponse("FollowLoadTest", response);

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