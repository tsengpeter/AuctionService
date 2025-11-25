using MemberService.API;
using MemberService.Application.DTOs.Auth;
using MemberService.Application.DTOs.Users;
using MemberService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace MemberService.IntegrationTests.API;

/// <summary>
/// Integration tests for UsersController endpoints
/// Tests user profile query functionality with real database
/// </summary>
public class UsersControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("memberservice_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _testUserToken = string.Empty;
    private long _testUserId;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _container.StartAsync();

        // Create factory with test database connection
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.FirstOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MemberDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<MemberDbContext>(options =>
                        options.UseNpgsql(_container.GetConnectionString()),
                        ServiceLifetime.Scoped);
                });
            });

        _client = _factory.CreateClient();

        // Setup test data - Register and login a test user
        await SetupTestUser();
    }

    private async Task SetupTestUser()
    {
        // Register test user
        var registerRequest = new RegisterRequest
        {
            Email = "testuser@example.com",
            Password = "TestPassword123",
            Username = "TestUser"
        };

        var registerResponse = await _client.PostAsync(
            "/api/auth/register",
            new StringContent(
                JsonSerializer.Serialize(registerRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        Assert.True(registerResponse.IsSuccessStatusCode, "Failed to register test user");

        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerData = JsonSerializer.Deserialize<JsonElement>(registerContent);
        _testUserToken = registerData.GetProperty("accessToken").GetString()!;
        _testUserId = registerData.GetProperty("userId").GetInt64();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await _container.StopAsync();
    }

    /// <summary>
    /// Test T096: GET /api/users/me returns authenticated user's full profile
    /// </summary>
    [Fact]
    public async Task GetMyProfile_WithValidToken_ReturnsUserProfile()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<UserProfileResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(profile);
        Assert.Equal(_testUserId, profile.UserId);
        Assert.Equal("testuser@example.com", profile.Email);
        Assert.Equal("TestUser", profile.Username);
        Assert.NotEqual(default, profile.CreatedAt);
        Assert.NotEqual(default, profile.UpdatedAt);
    }

    /// <summary>
    /// Test T096: GET /api/users/me without token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetMyProfile_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test T097: GET /api/users/{id} returns public profile for other user
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithValidId_ReturnsPublicProfile()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        // Create another user for testing
        var otherUserRequest = new RegisterRequest
        {
            Email = "otheruser@example.com",
            Password = "OtherPassword123",
            Username = "OtherUser"
        };

        var registerResponse = await _client.PostAsync(
            "/api/auth/register",
            new StringContent(
                JsonSerializer.Serialize(otherUserRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerData = JsonSerializer.Deserialize<JsonElement>(registerContent);
        var otherUserId = registerData.GetProperty("userId").GetInt64();

        // Act - Get public profile of other user
        var response = await _client.GetAsync($"/api/users/{otherUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var publicProfile = JsonSerializer.Deserialize<PublicUserProfileResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(publicProfile);
        Assert.Equal(otherUserId, publicProfile.UserId);
        Assert.Equal("OtherUser", publicProfile.Username);
        Assert.NotEqual(default, publicProfile.CreatedAt);

        // Email should NOT be returned for public profile
        // Verify by checking that the JSON doesn't contain an "email" property
        Assert.DoesNotContain("email", content.ToLower());
    }

    /// <summary>
    /// Test T097: GET /api/users/{id} with non-existent user returns 404
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        var nonExistentUserId = 999999L;

        // Act
        var response = await _client.GetAsync($"/api/users/{nonExistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test T097: GET /api/users/{id} without token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/users/{_testUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test T110: PUT /api/users/me updates profile successfully
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithValidRequest_Returns200Updated()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        var updateRequest = new UpdateProfileRequest
        {
            Username = "Updated Username",
            Email = "newemail@example.com"
        };

        // Act
        var response = await _client.PutAsync(
            "/api/users/me",
            new StringContent(
                JsonSerializer.Serialize(updateRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<UserProfileResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(profile);
        Assert.Equal("Updated Username", profile.Username);
        Assert.Equal("newemail@example.com", profile.Email);
    }

    /// <summary>
    /// Test T110: PUT /api/users/me with duplicate email returns 400
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithDuplicateEmail_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        // Create another user
        var anotherUserRequest = new RegisterRequest
        {
            Email = "another@example.com",
            Password = "AnotherPassword123",
            Username = "AnotherUser"
        };

        await _client.PostAsync(
            "/api/auth/register",
            new StringContent(
                JsonSerializer.Serialize(anotherUserRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Try to update first user with second user's email
        var updateRequest = new UpdateProfileRequest
        {
            Email = "another@example.com"
        };

        // Act
        var response = await _client.PutAsync(
            "/api/users/me",
            new StringContent(
                JsonSerializer.Serialize(updateRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test T110: PUT /api/users/me with invalid username returns 400
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithInvalidUsername_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        var updateRequest = new UpdateProfileRequest
        {
            Username = "ab", // Too short
            Email = "valid@example.com"
        };

        // Act
        var response = await _client.PutAsync(
            "/api/users/me",
            new StringContent(
                JsonSerializer.Serialize(updateRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test T111: PUT /api/users/me/password changes password successfully
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithValidRequest_Returns204()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        var changePasswordRequest = new ChangePasswordRequest
        {
            OldPassword = "TestPassword123",
            NewPassword = "NewSecurePassword456"
        };

        // Act
        var response = await _client.PutAsync(
            "/api/users/me/password",
            new StringContent(
                JsonSerializer.Serialize(changePasswordRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify new password works by logging in with it
        var loginRequest = new LoginRequest
        {
            Email = "testuser@example.com",
            Password = "NewSecurePassword456"
        };

        var loginResponse = await _client.PostAsync(
            "/api/auth/login",
            new StringContent(
                JsonSerializer.Serialize(loginRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        Assert.True(loginResponse.IsSuccessStatusCode, "New password should work for login");
    }

    /// <summary>
    /// Test T111: PUT /api/users/me/password with wrong old password returns 400
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        var changePasswordRequest = new ChangePasswordRequest
        {
            OldPassword = "WrongPassword",
            NewPassword = "NewSecurePassword456"
        };

        // Act
        var response = await _client.PutAsync(
            "/api/users/me/password",
            new StringContent(
                JsonSerializer.Serialize(changePasswordRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test T111: PUT /api/users/me/password with short new password returns 400
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithShortNewPassword_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _testUserToken);

        var changePasswordRequest = new ChangePasswordRequest
        {
            OldPassword = "TestPassword123",
            NewPassword = "Short"
        };

        // Act
        var response = await _client.PutAsync(
            "/api/users/me/password",
            new StringContent(
                JsonSerializer.Serialize(changePasswordRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test T111: PUT /api/users/me/password without token returns 401
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var changePasswordRequest = new ChangePasswordRequest
        {
            OldPassword = "TestPassword123",
            NewPassword = "NewSecurePassword456"
        };

        // Act
        var response = await _client.PutAsync(
            "/api/users/me/password",
            new StringContent(
                JsonSerializer.Serialize(changePasswordRequest),
                MediaTypeHeaderValue.Parse("application/json")));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
