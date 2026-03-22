using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace AuctionService.IntegrationTests.Health;

[Collection("Integration")]
public class HealthCheckTests
{
    private readonly IntegrationTestFixture _fixture;

    public HealthCheckTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _fixture.Client.GetAsync("/health");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        sw.ElapsedMilliseconds.Should().BeLessThan(100, "SC-006: health check must respond within 100ms");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthyStatus()
    {
        var response = await _fixture.Client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Healthy");
    }
}
