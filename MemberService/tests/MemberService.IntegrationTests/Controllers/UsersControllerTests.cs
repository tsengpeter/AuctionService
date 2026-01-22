using FluentAssertions;
using MemberService.Application.DTOs.Auth;
using MemberService.Application.DTOs.Users;
using MemberService.IntegrationTests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using MemberService.API;

namespace MemberService.IntegrationTests.Controllers;

public class UsersControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testDatabaseName;

    public UsersControllerTests()
    {
        _testDatabaseName = $"usertest_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        TestDatabaseHelper.EnsureDatabaseStartedAsync().Wait();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:Redis"] = TestDatabaseHelper.GetRedisConnectionString(),
                    ["Jwt:Issuer"] = "MemberService",
                    ["Jwt:Audience"] = "MemberService",
                    ["Jwt:SecretKey"] = "your-super-secret-key-here-change-in-production",
                    ["Jwt:ExpiryInMinutes"] = "15",
                    ["RefreshToken:ExpiryInDays"] = "7"
                });
            });
            builder.ConfigureServices(services =>
            {
                TestDatabaseHelper.ConfigureTestDatabase(services, _testDatabaseName);
            });
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ReturnsPublicProfile()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "TestPassword123!",
            PhoneNumber = "+886912345678"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        registerResult.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync($"/api/users/{registerResult!.User.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserPublicProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(registerResult.User.Id);
        profile.Username.Should().Be(registerResult.User.Username);
        profile.CreatedAt.Should().BeCloseTo(registerResult.User.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        // Act
        var response = await _client.GetAsync("/api/users/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMe_WhenAuthenticated_ReturnsUserProfile()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "me@example.com",
            Username = "meuser",
            Password = "TestPassword123!",
            PhoneNumber = "+886912345678"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login to get tokens
        var loginRequest = new LoginRequest
        {
            Email = "me@example.com",
            Password = "TestPassword123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();

        // Set authorization header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(authResponse.User.Id);
        profile.Email.Should().Be(authResponse.User.Email);
        profile.Username.Should().Be(authResponse.User.Username);
    }

    [Fact]
    public async Task GetMe_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMyProfile_WhenAuthenticated_UpdatesUsername_ReturnsUpdatedProfile()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "update@example.com",
            Username = "updateuser",
            Password = "TestPassword123!",
            PhoneNumber = "+886912345678"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login to get tokens
        var loginRequest = new LoginRequest
        {
            Email = "update@example.com",
            Password = "TestPassword123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var updateRequest = new UpdateProfileRequest { Username = "updateduser" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(authResponse.User.Id);
        profile.Email.Should().Be(authResponse.User.Email);
        profile.Username.Should().Be("updateduser");
    }

    [Fact]
    public async Task UpdateMyProfile_WhenAuthenticated_UpdatesEmail_ReturnsUpdatedProfile()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "update2@example.com",
            Username = "updateuser",
            Password = "TestPassword123!",
            PhoneNumber = "+886912345678"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login to get tokens
        var loginRequest = new LoginRequest
        {
            Email = "update2@example.com",
            Password = "TestPassword123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var updateRequest = new UpdateProfileRequest { Email = "updated@example.com" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(authResponse.User.Id);
        profile.Email.Should().Be("updated@example.com");
        profile.Username.Should().Be(authResponse.User.Username);
    }

    [Fact]
    public async Task UpdateMyProfile_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var updateRequest = new UpdateProfileRequest { Username = "updateduser" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WhenAuthenticated_ValidOldPassword_ReturnsNoContent()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "password@example.com",
            Username = "passworduser",
            Password = "OldPassword123!",
            PhoneNumber = "+886912345678"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login to get tokens
        var loginRequest = new LoginRequest
        {
            Email = "password@example.com",
            Password = "OldPassword123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var changeRequest = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/me/password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WhenAuthenticated_InvalidOldPassword_ReturnsBadRequest()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "password2@example.com",
            Username = "passworduser",
            Password = "OldPassword123!",
            PhoneNumber = "+886912345678"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login to get tokens
        var loginRequest = new LoginRequest
        {
            Email = "password2@example.com",
            Password = "OldPassword123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var changeRequest = new ChangePasswordRequest
        {
            OldPassword = "WrongPassword123!",
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/me/password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await TestDatabaseHelper.EnsureDatabaseStartedAsync();
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var changeRequest = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/me/password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
