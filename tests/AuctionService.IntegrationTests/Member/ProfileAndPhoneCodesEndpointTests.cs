using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class ProfileAndPhoneCodesEndpointTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    [Fact]
    public async Task GET_Me_WithValidToken_Returns200_WithUserDto()
    {
        var token = await RegisterAndLoginAsync();
        token.Should().NotBeNull();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await Client.GetAsync("/api/users/me");
        Client.DefaultRequestHeaders.Authorization = null;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.Data.Should().NotBeNull();
        body.Data!.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GET_Me_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_PhoneCodes_Returns200_WithSeedData()
    {
        var response = await Client.GetAsync("/api/phone-country-codes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PhoneCodesResponse>();
        body!.Data.Should().NotBeNull();
        body.Data.Should().NotBeEmpty();
        body.Data!.Should().Contain(p => p.DialCode == "886");
    }

    [Fact]
    public async Task GET_PhoneCodes_DoesNotRequireAuth()
    {
        // No auth header needed
        var response = await Client.GetAsync("/api/phone-country-codes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record UserResponse(bool Success, UserData? Data, int StatusCode);
    private record UserData(Guid Id, string Email, string Username);
    private record PhoneCodesResponse(bool Success, PhoneCodeData[]? Data, int StatusCode);
    private record PhoneCodeData(int Id, string DialCode, string CountryName, string CountryIso);
}
