using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class UpdateProfileEndpointTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    [Fact]
    public async Task PUT_Me_WithValidData_Returns200_AndUpdatedUsername()
    {
        var result = await RegisterAndGetTokensAsync();
        var token = result!.Value.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUsername = "updated_user_" + Guid.NewGuid().ToString("N")[..6];
        var request = new { Username = newUsername, DisplayName = "Updated Name" };
        var response = await Client.PutAsJsonAsync("/api/users/me", request);
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpdateResponse>();
        body!.Data!.Username.Should().Be(newUsername);
    }

    [Fact]
    public async Task PUT_Me_WithDuplicateUsername_Returns409()
    {
        // Register two users with known usernames
        var user1Name = "dupuser_" + Guid.NewGuid().ToString("N")[..6];
        var user2Name = "dupuser_" + Guid.NewGuid().ToString("N")[..6];
        var email1 = $"dup1_{Guid.NewGuid():N}@example.com";
        var email2 = $"dup2_{Guid.NewGuid():N}@example.com";

        await Client.PostAsJsonAsync("/api/auth/register", BuildRegisterRequest(email1, user1Name));
        var login2Result = await RegisterAndGetTokensAsync(email: email2, username: user2Name);
        var token2 = login2Result!.Value.AccessToken;

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var response = await Client.PutAsJsonAsync("/api/users/me", new { Username = user1Name });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static object BuildRegisterRequest(string email, string username) => new
    {
        email,
        username,
        password = DefaultPassword,
        phoneCountryCodeId = 1,
        phoneNumber = "912345678",
        address = new { country = (string?)null, city = (string?)null, postalCode = (string?)null, addressLine = (string?)null }
    };

    [Fact]
    public async Task PUT_Me_WithTooLongUsername_Returns422()
    {
        var result = await RegisterAndGetTokensAsync();
        var token = result!.Value.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PutAsJsonAsync("/api/users/me", new { Username = new string('a', 31) });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PUT_Me_WithPhoneField_Returns422()
    {
        var result = await RegisterAndGetTokensAsync();
        var token = result!.Value.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PutAsJsonAsync("/api/users/me", new { Username = "validuser1", Phone = "912345678" });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PUT_Me_WithEmailField_Returns422()
    {
        var result = await RegisterAndGetTokensAsync();
        var token = result!.Value.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PutAsJsonAsync("/api/users/me", new { Username = "validuser2", Email = "new@email.com" });
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PUT_Me_WithoutAuth_Returns401()
    {
        var response = await Client.PutAsJsonAsync("/api/users/me", new { Username = "noauth" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record UpdateResponse(bool Success, UserData? Data, int StatusCode);
    private record UserData(Guid Id, string Username, string Email);
}
