using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class ChangePasswordEndpointTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    [Fact]
    public async Task PUT_Password_WithCorrectCurrentPassword_Returns204()
    {
        var result = await RegisterAndGetTokensAsync();
        var accessToken = result!.Value.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await Client.PutAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = DefaultPassword,
            NewPassword = "NewSecure@456"
        });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PUT_Password_RevokesAllRefreshTokens()
    {
        var result = await RegisterAndGetTokensAsync();
        var accessToken = result!.Value.AccessToken;
        var refreshToken = result!.Value.RefreshToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await Client.PutAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = DefaultPassword,
            NewPassword = "NewSecure@456"
        });
        Client.DefaultRequestHeaders.Authorization = null;

        // Old refresh token should no longer work
        var refreshResp = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_Password_WithWrongCurrentPassword_Returns401()
    {
        var result = await RegisterAndGetTokensAsync();
        var accessToken = result!.Value.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await Client.PutAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = "WrongPass!999",
            NewPassword = "NewSecure@456"
        });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_Password_WithSameNewPassword_Returns422()
    {
        var result = await RegisterAndGetTokensAsync();
        var accessToken = result!.Value.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await Client.PutAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = DefaultPassword,
            NewPassword = DefaultPassword
        });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PUT_Password_WithoutAuth_Returns401()
    {
        var response = await Client.PutAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = DefaultPassword,
            NewPassword = "NewSecure@456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
