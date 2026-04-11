using System.Net;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class LogoutEndpointTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    [Fact]
    public async Task POST_Logout_ValidToken_Returns204()
    {
        var tokens = await RegisterAndGetTokensAsync();
        tokens.Should().NotBeNull();

        var response = await Client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = tokens!.Value.RefreshToken });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_Logout_Idempotent_Returns204_OnRepeat()
    {
        var tokens = await RegisterAndGetTokensAsync();
        tokens.Should().NotBeNull();

        var r1 = await Client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = tokens!.Value.RefreshToken });
        r1.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var r2 = await Client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = tokens.Value.RefreshToken });
        r2.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_Logout_ThenRefresh_Returns401()
    {
        var tokens = await RegisterAndGetTokensAsync();
        tokens.Should().NotBeNull();

        await Client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = tokens!.Value.RefreshToken });

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.Value.RefreshToken });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
