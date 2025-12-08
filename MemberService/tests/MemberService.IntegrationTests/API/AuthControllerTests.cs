using FluentAssertions;
using MemberService.Application.DTOs.Auth;
using MemberService.IntegrationTests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using MemberService.API;

namespace MemberService.IntegrationTests.API;

public class AuthControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testDatabaseName;

    public AuthControllerTests()
    {
        _testDatabaseName = $"authtest_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        TestDatabaseHelper.EnsureDatabaseStartedAsync().Wait();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Issuer"] = "MemberService",
                    ["Jwt:Audience"] = "MemberService",
                    ["Jwt:SecretKey"] = "your-super-secret-jwt-key-min-32-chars-long-for-hs256-algorithm",
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
    public async Task Register_WithValidData_ReturnsCreatedWithAuthResponse()
    {
        // Arrange
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.User.Should().NotBeNull();
        authResponse.User.Email.Should().Be(registerRequest.Email);
        authResponse.User.Username.Should().Be(registerRequest.Username);
        authResponse.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest1 = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser1",
            Password = "TestPassword123!"
        };

        var registerRequest2 = new RegisterRequest
        {
            Email = "test@example.com", // Same email
            Username = "testuser2",
            Password = "TestPassword123!"
        };

        // First registration
        var response1 = await _client.PostAsJsonAsync("/api/auth/register", registerRequest1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Second registration with same email
        var response2 = await _client.PostAsJsonAsync("/api/auth/register", registerRequest2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthResponse()
    {
        // Arrange
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest = new RegisterRequest
        {
            Email = "login@example.com",
            Username = "loginuser",
            Password = "TestPassword123!"
        };

        // Register first
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.User.Should().NotBeNull();
        authResponse.User.Email.Should().Be(registerRequest.Email);
        authResponse.User.Username.Should().Be(registerRequest.Username);
        authResponse.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
