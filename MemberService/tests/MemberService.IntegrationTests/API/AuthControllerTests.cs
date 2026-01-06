using FluentAssertions;
using MemberService.Application.DTOs.Auth;
using MemberService.IntegrationTests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MemberService.Domain.Interfaces;
using MemberService.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
                // Add test configuration with higher priority (added last)
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:MemberDb"] = TestDatabaseHelper.GetConnectionString(_testDatabaseName),
                    ["Jwt:Issuer"] = "MemberService",
                    ["Jwt:Audience"] = "MemberService",
                    ["Jwt:SecretKey"] = "your-super-secret-key-here-change-in-production",
                    ["Jwt:ExpiryInMinutes"] = "60"
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
    public async Task Register_WithValidData_ReturnsCreatedWithRegisterResponse()
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
        var registerResponse = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        registerResponse.Should().NotBeNull();
        registerResponse!.User.Should().NotBeNull();
        registerResponse.User.Email.Should().Be(registerRequest.Email);
        registerResponse.User.Username.Should().Be(registerRequest.Username);
        registerResponse.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        var registerRequest1 = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "TestPassword123!"
        };

        var registerRequest2 = new RegisterRequest
        {
            Email = "test@example.com", // Same email
            Username = "anothertestuser",
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

    [Fact]
    public async Task Validate_ValidToken_ShouldReturnValidResponse()
    {
        // Arrange
        await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services, _testDatabaseName);

        // First register user
        var registerRequest = new RegisterRequest
        {
            Email = "validate@example.com",
            Password = "ValidPassword123!",
            Username = "validateuser"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Then login to get a valid token
        var loginRequest = new LoginRequest
        {
            Email = "validate@example.com",
            Password = "ValidPassword123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();

        // Act - Validate the token
        var validateResponse = await _client.GetAsync($"/api/auth/validate?token={loginResult.AccessToken}");

        // Assert
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validateResult = await validateResponse.Content.ReadFromJsonAsync<TokenValidationResponse>();
        validateResult.Should().NotBeNull();
        validateResult!.IsValid.Should().BeTrue();
        validateResult.UserId.Should().NotBeNull();
        validateResult.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Validate_InvalidToken_ShouldReturnInvalidResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/auth/validate?token=invalid_token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<TokenValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Validate_NoToken_ShouldReturnInvalidResponse()
    {
        // Arrange - No token parameter

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_EmptyToken_ShouldReturnInvalidResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/auth/validate?token=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_MalformedToken_ShouldReturnInvalidResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/auth/validate?token=malformed_token_xyz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<TokenValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Validate_ExpiredToken_ShouldReturnInvalidResponse()
    {
        // Arrange - Create an expired token manually
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var response = await _client.GetAsync($"/api/auth/validate?token={expiredToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<TokenValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Validate_TamperedToken_ShouldReturnInvalidResponse()
    {
        // Arrange - Valid token with tampered signature
        var tamperedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.tampered_signature";

        // Act
        var response = await _client.GetAsync($"/api/auth/validate?token={tamperedToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<TokenValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
    }
}
