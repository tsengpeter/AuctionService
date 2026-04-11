using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using Member.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public abstract class MemberIntegrationTestBase(IntegrationTestFixture fixture)
{
    protected HttpClient Client => fixture.Client;

    protected async Task<string?> RegisterAndLoginAsync(
        string? email = null,
        string? username = null,
        string? password = null)
    {
        email ??= $"user_{Guid.NewGuid():N}@example.com";
        username ??= $"user{Guid.NewGuid().ToString("N")[..8]}";
        password ??= "Password1!";

        var reg = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            username,
            password,
            phoneCountryCodeId = 1,
            phoneNumber = "912345678",
            address = new { country = (string?)null, city = (string?)null, postalCode = (string?)null, addressLine = (string?)null }
        });

        if (!reg.IsSuccessStatusCode)
            return null;

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (!login.IsSuccessStatusCode)
            return null;

        var loginResult = await login.Content.ReadFromJsonAsync<LoginResult>();
        return loginResult?.Data?.AccessToken;
    }

    protected async Task<(string AccessToken, string RefreshToken)?> RegisterAndGetTokensAsync(
        string? email = null,
        string? username = null,
        string? password = null)
    {
        email ??= $"user_{Guid.NewGuid():N}@example.com";
        username ??= $"user{Guid.NewGuid().ToString("N")[..8]}";
        password ??= "Password1!";

        var reg = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            username,
            password,
            phoneCountryCodeId = 1,
            phoneNumber = "912345678",
            address = new { country = (string?)null, city = (string?)null, postalCode = (string?)null, addressLine = (string?)null }
        });

        if (!reg.IsSuccessStatusCode)
            return null;

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (!login.IsSuccessStatusCode)
            return null;

        var loginResult = await login.Content.ReadFromJsonAsync<LoginResult>();
        if (loginResult?.Data == null) return null;

        return (loginResult.Data.AccessToken, loginResult.Data.RefreshToken);
    }

    protected const string DefaultPassword = "Password1!";

    protected static HttpClient CreateAuthorizedClient(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record LoginResult(bool Success, TokenData? Data, int StatusCode);
    private record TokenData(string AccessToken, string RefreshToken, int ExpiresIn);
}

