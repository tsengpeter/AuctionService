using System.Net;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class LoginEndpointTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    [Fact]
    public async Task POST_Login_ValidCredentials_Returns200_WithTokens()
    {
        var email = $"login_{Guid.NewGuid():N}@example.com";
        var password = "Password1!";

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email, username = $"user{Guid.NewGuid().ToString("N")[..8]}", password,
            phoneCountryCodeId = 1, phoneNumber = "912345678",
            address = new { country = (string?)null, city = (string?)null, postalCode = (string?)null, addressLine = (string?)null }
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_Login_WrongPassword_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@example.com", password = "WrongPass1!" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Login_UnknownEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { email = $"x_{Guid.NewGuid():N}@example.com", password = "Password1!" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record LoginResponse(bool Success, TokenData? Data, int StatusCode);
    private record TokenData(string AccessToken, string RefreshToken, int ExpiresIn);
}

