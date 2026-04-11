using System.Net;
using System.Net.Http.Json;
using AuctionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AuctionService.IntegrationTests.Member;

[Collection("Integration")]
public class RegisterEndpointTests(IntegrationTestFixture fixture) : MemberIntegrationTestBase(fixture)
{
    private static object ValidRegisterBody(string? email = null, string? username = null) => new
    {
        email = email ?? $"test_{Guid.NewGuid():N}@example.com",
        username = username ?? $"user{Guid.NewGuid().ToString("N")[..8]}",
        password = "Password1!",
        displayName = (string?)null,
        phoneCountryCodeId = 1,
        phoneNumber = "912345678",
        address = new { country = (string?)null, city = (string?)null, postalCode = (string?)null, addressLine = (string?)null }
    };

    [Fact]
    public async Task POST_Register_ValidRequest_Returns201_WithUserDto()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<UserDtoResponse>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_Register_DuplicateEmail_Returns409()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody(email: email));
        var response = await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody(email: email));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_Register_WeakPassword_Returns422()
    {
        var body = new
        {
            email = $"test_{Guid.NewGuid():N}@example.com",
            username = $"user{Guid.NewGuid().ToString("N")[..6]}",
            password = "weak",
            phoneCountryCodeId = 1,
            phoneNumber = "912345678",
            address = new { country = (string?)null, city = (string?)null, postalCode = (string?)null, addressLine = (string?)null }
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", body);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_Register_InvalidPhoneCountryCodeId_Returns422()
    {
        var body = new
        {
            email = $"test_{Guid.NewGuid():N}@example.com",
            username = $"user{Guid.NewGuid().ToString("N")[..6]}",
            password = "Password1!",
            phoneCountryCodeId = 99999,
            phoneNumber = "912345678",
            address = new { country = (string?)null, city = (string?)null, postalCode = (string?)null, addressLine = (string?)null }
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", body);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private record ApiResponseWrapper<T>(bool Success, T? Data, int StatusCode);
    private record UserDtoResponse(Guid Id, string Email, string Username, string? DisplayName);
}

