using System.Net;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class RefreshTokenEndpointTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    [Fact]
    public async Task POST_Refresh_ValidToken_Returns200_WithNewTokens()
    {
        var tokens = await RegisterAndGetTokensAsync();
        tokens.Should().NotBeNull();

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens!.Value.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_Refresh_InvalidToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "invalid_raw_token" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Refresh_AlreadyUsedToken_Returns401()
    {
        var tokens = await RegisterAndGetTokensAsync();
        tokens.Should().NotBeNull();

        // Use the token once
        var r1 = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens!.Value.RefreshToken });
        r1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use again - should fail (token was revoked)
        var r2 = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.Value.RefreshToken });
        r2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record LoginResponse(bool Success, TokenData? Data, int StatusCode);
    private record TokenData(string AccessToken, string RefreshToken, int ExpiresIn);
}
